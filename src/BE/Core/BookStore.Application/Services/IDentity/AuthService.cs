using BookStore.Application.Dtos.IdentityDto;
using BookStore.Application.IService.Identity;
using BookStore.Domain.Entities.Identity;
using BookStore.Domain.IRepository.Common;
using BookStore.Shared.Common;
using BookStore.Shared.Errors;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.Services.IDentity
{
    public class AuthService : IAuthService
    {
        private readonly IUnitOfWork _uow;
        private readonly IJwtService _jwt;
        private readonly IHashingService _hashing;
        private readonly IEmailSender _email;

        public AuthService(IUnitOfWork uow, IJwtService jwt, IHashingService hashing, IEmailSender email)
        {
            _uow = uow;
            _jwt = jwt;
            _hashing = hashing;
            _email = email;
        }
        #region comment ConfirmEmail
        //public async Task<BaseResult<string>> ConfirmEmailAsync(Guid userId, string tokenPlain)
        //{
        //    if (userId == Guid.Empty || string.IsNullOrWhiteSpace(tokenPlain))
        //        return BaseResult<string>.Fail("Auth.InvalidRequest", "Invalid request", ErrorType.Validation);

        //    var user = await _uow.Users.GetByIdAsync(userId);
        //    if (user == null)
        //        return BaseResult<string>.Fail(AuthErrors.InvalidCredentials);

        //    var tokenHash = _hashing.HashToken(tokenPlain);

        //    var token = await _uow.EmailVerificationTokens
        //        .GetFirstOrDefaultAsync(x => x.UserId == userId && x.TokenHash == tokenHash);

        //    if (token == null || token.ExpiresAt < DateTime.UtcNow)
        //        return BaseResult<string>.Fail("Auth.InvalidToken", "Invalid or expired token", ErrorType.Validation);

        //    return await BaseResult<string>.Create(async () =>
        //    {
        //        await _uow.ExcuteTransactionAsync(async () =>
        //        {
        //            user.EmailConfirmed = true;

        //            _uow.Users.Update(user);
        //            _uow.EmailVerificationTokens.Delete(token);

        //            await _uow.SaveChangesAsync();
        //        });

        //        return "Email confirmed successfully.";
        //    });
        //}
        #endregion

        public async Task<BaseResult<string>> ForgotPasswordAsync(string email, string clientUrl)
        {
            if (string.IsNullOrWhiteSpace(email))
                return BaseResult<string>.Fail("Auth.EmailRequired", "Email is required", ErrorType.Validation);

            return await BaseResult<string>.Create(async () =>
            {
                var user = await _uow.Users.GetFirstOrDefaultAsync(x => x.Email == email);

                // Không reveal user tồn tại
                if (user == null)
                    return "If your email exists, a reset link has been sent.";

                await _uow.ExecuteTransactionAsync(async () =>
                {
                    var tokenPlain = Guid.NewGuid().ToString("N");
                    var tokenHash = _hashing.HashToken(tokenPlain);

                    await _uow.PasswordResetTokens.AddAsync(new PasswordResetToken
                    {
                        TokenHash = tokenHash,
                        ExpiresAt = DateTime.UtcNow.AddHours(1),
                        UserId = user.Id,
                        Used = false
                    });

                    await _uow.SaveChangesAsync();

                    var resetUrl = $"{clientUrl.TrimEnd('/')}/reset-password?userId={user.Id}&token={tokenPlain}";
                    await _email.SendEmailAsync(user.Email, "Reset Password", $"Click here: {resetUrl}");
                });

                return "If your email exists, a reset link has been sent.";
            });
        }


        public async Task<BaseResult<AuthDto.AuthResponseDto>> LoginAsync(AuthDto.LoginDto dto, string originIp)
        {
            var user = await _uow.Users.GetFirstOrDefaultAsync(x => x.Email == dto.Email);

            if (user == null || !_hashing.VerifyPassword(dto.Password, user.PasswordHash))
                return BaseResult<AuthDto.AuthResponseDto>.Fail(AuthErrors.InvalidCredentials);
            #region Tam Bo check email
            //if (!user.EmailConfirmed)
            //    return BaseResult<AuthDto.AuthResponseDto>.Fail("Auth.EmailNotConfirmed", "Email chưa được xác nhận", ErrorType.Forbidden);
            #endregion
            return await BaseResult<AuthDto.AuthResponseDto>.Create(async () =>
            {
                // 🔥 Lấy roles của user
                var roles = await _uow.UserRoles.GetRolesByUserId(user.Id);
                var roleNames = roles.Select(r => r.Name);

                // 🔥 Truyền roles vào token
                var access = _jwt.GenerateAccessToken(user, roleNames);

                var (refreshPlain, refreshExpiry) = _jwt.GenerateRefreshToken();

                await _uow.RefreshTokens.AddAsync(new RefreshToken
                {
                    UserId = user.Id,
                    TokenHash = _hashing.HashToken(refreshPlain),
                    ExpiresAt = refreshExpiry,
                    CreatedAt = DateTime.UtcNow,
                    CreatedByIp = originIp
                });

                await _uow.SaveChangesAsync();

                return new AuthDto.AuthResponseDto(access, refreshPlain, user.Email, refreshExpiry);
            });

        }


        public async Task<BaseResult<string>> LogoutAsync(string refreshTokenPlain)
        {
            if (string.IsNullOrWhiteSpace(refreshTokenPlain))
                return BaseResult<string>.Fail("Auth.InvalidToken", "Refresh token is required", ErrorType.Validation);

            return await BaseResult<string>.Create(async () =>
            {
                var hash = _hashing.HashToken(refreshTokenPlain);
                var rt = await _uow.RefreshTokens.GetFirstOrDefaultAsync(x => x.TokenHash == hash);

                if (rt != null && !rt.Revoked)
                {
                    rt.Revoked = true;
                    await _uow.SaveChangesAsync();
                }

                return "Logged out successfully";
            });
        }



        public async Task<BaseResult<AuthDto.AuthResponseDto>> RefreshTokenAsync(string refreshToken, string originIp)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
                return BaseResult<AuthDto.AuthResponseDto>.Fail("Auth.InvalidToken", "Refresh token is required", ErrorType.Validation);

            var hash = _hashing.HashToken(refreshToken);
            var existing = await _uow.RefreshTokens.GetFirstOrDefaultAsync(x => x.TokenHash == hash);

            if (existing == null || existing.Revoked || existing.ExpiresAt < DateTime.UtcNow)
                return BaseResult<AuthDto.AuthResponseDto>.Fail("Auth.InvalidToken", "Invalid or expired refresh token", ErrorType.Unauthorized);

            // 🔥 Lấy user kèm role
            var user = await _uow.Users.GetByIdAsync(existing.UserId);
            if (user == null)
                return BaseResult<AuthDto.AuthResponseDto>.Fail("Auth.InvalidUser", "User not found", ErrorType.NotFound);

            var roles = await _uow.UserRoles.GetRolesByUserId(user.Id);
            var roleNames = roles.Select(r => r.Name);

            var (newPlain, newExpiry) = _jwt.GenerateRefreshToken();
            var newHash = _hashing.HashToken(newPlain);

            await _uow.ExecuteTransactionAsync(async () =>
            {
                existing.Revoked = true;
                existing.ReplacedByTokenHash = newHash;

                await _uow.RefreshTokens.AddAsync(new RefreshToken
                {
                    TokenHash = newHash,
                    UserId = existing.UserId,
                    ExpiresAt = newExpiry,
                    CreatedAt = DateTime.UtcNow,
                    CreatedByIp = originIp
                });

                await _uow.SaveChangesAsync();
            });

            // 🔥 Access token mới phải chứa role
            var access = _jwt.GenerateAccessToken(user, roleNames);

            return BaseResult<AuthDto.AuthResponseDto>.Ok(
                new AuthDto.AuthResponseDto(access, newPlain, user.Email, newExpiry)
            );
        }




        public async Task<BaseResult<string>> RegisterAsync(AuthDto.RegisterDto dto, string originIp)
        {
            var exist = await _uow.Users.GetFirstOrDefaultAsync(x => x.Email == dto.Email);
            if (exist != null)
                return BaseResult<string>.Fail(AuthErrors.EmailExists);

            return await BaseResult<string>.Create(async () =>
            {
                await _uow.ExecuteTransactionAsync(async () =>
                {
                    var user = new User
                    {
                        Email = dto.Email,
                        PasswordHash = _hashing.HashPassword(dto.Password),
                        EmailConfirmed = true  //(Mặc định đã xác thực)
                    };

                    await _uow.Users.AddAsync(user);

                    // Assign customer role
                    var customerRole = await _uow.Roles.GetFirstOrDefaultAsync(r => r.Name == "Customer");
                    if (customerRole != null)
                    {
                        await _uow.UserRoles.AddAsync(new UserRole
                        {
                            UserId = user.Id,
                            RoleId = customerRole.Id
                        });
                    }
                    #region BỎ ĐOẠN TẠO TOKEN VÀ GỬI EMAIL
                    
                    /*
                    var tokenPlain = Guid.NewGuid().ToString("N");
                    var tokenHash = _hashing.HashToken(tokenPlain);

                    await _uow.EmailVerificationTokens.AddAsync(new EmailVerificationToken
                    {
                        TokenHash = tokenHash,
                        ExpiresAt = DateTime.UtcNow.AddHours(24),
                        UserId = user.Id
                    });

                    var verifyUrl = $"{clientUrl.TrimEnd('/')}/verify-email?userId={user.Id}&token={tokenPlain}";
                    await _email.SendEmailAsync(dto.Email, "Verify email", $"Click here: {verifyUrl}");
                    */
                    
                    #endregion
                    await _uow.SaveChangesAsync();
                });

                return "Registered successfully."; // Đổi thông báo trả về
            });
        }
        public async Task<BaseResult<string>> CreateUserByAdminAsync(AuthDto.CreateUserByAdminDto dto)
        {
            // 1. Kiểm tra Email tồn tại chưa
            var existUser = await _uow.Users.GetFirstOrDefaultAsync(x => x.Email == dto.Email);
            if (existUser != null)
                return BaseResult<string>.Fail(AuthErrors.EmailExists);

            // 2. Kiểm tra Role có tồn tại không (Quan trọng)
            var role = await _uow.Roles.GetFirstOrDefaultAsync(r => r.Name == dto.RoleName);
            if (role == null)
                return BaseResult<string>.Fail("Auth.RoleNotFound", $"Role '{dto.RoleName}' does not exist.", ErrorType.NotFound);

            return await BaseResult<string>.Create(async () =>
            {
                await _uow.ExecuteTransactionAsync(async () =>
                {
                    // 3. Tạo User
                    var user = new User
                    {
                        Email = dto.Email,
                        PasswordHash = _hashing.HashPassword(dto.Password),
                        EmailConfirmed = true, // Admin tạo thì mặc định đã xác thực
                        IsActive = true
                    };

                    await _uow.Users.AddAsync(user);

                    // 4. Gán Role cho User
                    await _uow.UserRoles.AddAsync(new UserRole
                    {
                        UserId = user.Id,
                        RoleId = role.Id
                    });

                    await _uow.SaveChangesAsync();
                });

                return $"User {dto.Email} created successfully with role {dto.RoleName}.";
            });
        }

        public async Task<BaseResult<string>> ResetPasswordAsync(AuthDto.ResetPasswordDto dto)
        {
            if (dto == null || dto.UserId == Guid.Empty ||
                string.IsNullOrWhiteSpace(dto.Token) ||
                string.IsNullOrWhiteSpace(dto.NewPassword))
                return BaseResult<string>.Fail("Auth.InvalidRequest", "Invalid input", ErrorType.Validation);

            var tokenHash = _hashing.HashToken(dto.Token);

            var resetToken = await _uow.PasswordResetTokens
                .GetFirstOrDefaultAsync(x => x.UserId == dto.UserId && x.TokenHash == tokenHash);

            if (resetToken == null || resetToken.ExpiresAt < DateTime.UtcNow || resetToken.Used)
                return BaseResult<string>.Fail("Auth.InvalidToken", "Invalid or expired reset token", ErrorType.Validation);

            return await BaseResult<string>.Create(async () =>
            {
                await _uow.ExecuteTransactionAsync(async () =>
                {
                    var user = resetToken.User;

                    user.PasswordHash = _hashing.HashPassword(dto.NewPassword);
                    _uow.Users.Update(user);

                    resetToken.Used = true;
                    _uow.PasswordResetTokens.Update(resetToken);

                    await _uow.SaveChangesAsync();
                });

                return "Password reset successfully.";
            });
        }
    }
}


