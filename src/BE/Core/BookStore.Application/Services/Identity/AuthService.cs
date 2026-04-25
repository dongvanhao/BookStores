using BookStore.Application.Dtos.IdentityDto;
using BookStore.Application.IService.Identity;
using BookStore.Application.Options;
using BookStore.Domain.Entities.Identity;
using BookStore.Shared.Common;
using BookStore.Shared.Errors;
using Microsoft.Extensions.Options;
using BookStore.Domain.IRepository.Common;
using BookStore.Domain.IRepository.Identity;

namespace BookStore.Application.Services.Identity
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _users;
        private readonly IRoleRepository _roles;
        private readonly IUserRoleRepository _userRoles;
        private readonly IRefreshTokenRepository _refreshTokens;
        private readonly IGenericRepository<PasswordResetToken> _passwordResetTokens;
        private readonly IDbSession _session;
        private readonly IJwtService _jwt;
        private readonly IHashingService _hashing;
        private readonly IEmailSender _email;
        private readonly AppSettings _appSettings;

        public AuthService(
            IUserRepository users, IRoleRepository roles, IUserRoleRepository userRoles,
            IRefreshTokenRepository refreshTokens, IGenericRepository<PasswordResetToken> passwordResetTokens,
            IDbSession session, IJwtService jwt, IHashingService hashing, IEmailSender email,
            IOptions<AppSettings> appSettings)
        {
            _users = users;
            _roles = roles;
            _userRoles = userRoles;
            _refreshTokens = refreshTokens;
            _passwordResetTokens = passwordResetTokens;
            _session = session;
            _jwt = jwt;
            _hashing = hashing;
            _email = email;
            _appSettings = appSettings.Value;
        }

        public async Task<BaseResult<string>> ForgotPasswordAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return BaseResult<string>.Fail("Auth.EmailRequired", "Email is required", ErrorType.Validation);

            var user = await _users.GetFirstOrDefaultAsync(x => x.Email == email);

            // Prevent user enumeration — same message regardless of whether user exists
            if (user == null)
                return BaseResult<string>.Ok("If your email exists, a reset link has been sent.");

            await _session.ExecuteTransactionAsync(async () =>
            {
                var tokenPlain = Guid.NewGuid().ToString("N");
                var tokenHash = _hashing.HashToken(tokenPlain);

                await _passwordResetTokens.AddAsync(new PasswordResetToken
                {
                    TokenHash = tokenHash,
                    ExpiresAt = DateTime.UtcNow.AddHours(1),
                    UserId = user.Id,
                    Used = false
                });

                await _session.SaveChangesAsync();

                var resetUrl = $"{_appSettings.ClientBaseUrl.TrimEnd('/')}/reset-password?userId={user.Id}&token={tokenPlain}";
                await _email.SendEmailAsync(user.Email, "Reset Password", $"Click here: {resetUrl}");
            });

            return BaseResult<string>.Ok("If your email exists, a reset link has been sent.");
        }

        public async Task<BaseResult<AuthDto.AuthResponseDto>> LoginAsync(AuthDto.LoginDto dto, string originIp)
        {
            var user = await _users.GetFirstOrDefaultAsync(x => x.Email == dto.Email);

            if (user == null || !_hashing.VerifyPassword(dto.Password, user.PasswordHash))
                return BaseResult<AuthDto.AuthResponseDto>.Fail(AuthErrors.InvalidCredentials);

            var roles = await _userRoles.GetRolesByUserId(user.Id);
            var roleNames = roles.Select(r => r.Name);

            var access = _jwt.GenerateAccessToken(user, roleNames);
            var (refreshPlain, refreshExpiry) = _jwt.GenerateRefreshToken();

            await _refreshTokens.AddAsync(new RefreshToken
            {
                UserId = user.Id,
                TokenHash = _hashing.HashToken(refreshPlain),
                ExpiresAt = refreshExpiry,
                CreatedAt = DateTime.UtcNow,
                CreatedByIp = originIp
            });

            await _session.SaveChangesAsync();

            return BaseResult<AuthDto.AuthResponseDto>.Ok(
                new AuthDto.AuthResponseDto(access, refreshPlain, user.Email, refreshExpiry));
        }

        public async Task<BaseResult<string>> LogoutAsync(string refreshTokenPlain)
        {
            if (string.IsNullOrWhiteSpace(refreshTokenPlain))
                return BaseResult<string>.Fail("Auth.InvalidToken", "Refresh token is required", ErrorType.Validation);

            var hash = _hashing.HashToken(refreshTokenPlain);
            var rt = await _refreshTokens.GetFirstOrDefaultAsync(x => x.TokenHash == hash);

            if (rt != null && !rt.Revoked)
            {
                rt.Revoked = true;
                await _session.SaveChangesAsync();
            }

            return BaseResult<string>.Ok("Logged out successfully");
        }

        public async Task<BaseResult<AuthDto.AuthResponseDto>> RefreshTokenAsync(string refreshToken, string originIp)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
                return BaseResult<AuthDto.AuthResponseDto>.Fail("Auth.InvalidToken", "Refresh token is required", ErrorType.Validation);

            var hash = _hashing.HashToken(refreshToken);
            var existing = await _refreshTokens.GetFirstOrDefaultAsync(x => x.TokenHash == hash);

            if (existing == null || existing.Revoked || existing.ExpiresAt < DateTime.UtcNow)
                return BaseResult<AuthDto.AuthResponseDto>.Fail("Auth.InvalidToken", "Invalid or expired refresh token", ErrorType.Unauthorized);

            var user = await _users.GetByIdAsync(existing.UserId);
            if (user == null)
                return BaseResult<AuthDto.AuthResponseDto>.Fail("Auth.InvalidUser", "User not found", ErrorType.NotFound);

            var roles = await _userRoles.GetRolesByUserId(user.Id);
            var roleNames = roles.Select(r => r.Name);

            var (newPlain, newExpiry) = _jwt.GenerateRefreshToken();
            var newHash = _hashing.HashToken(newPlain);

            await _session.ExecuteTransactionAsync(async () =>
            {
                existing.Revoked = true;
                existing.ReplacedByTokenHash = newHash;

                await _refreshTokens.AddAsync(new RefreshToken
                {
                    TokenHash = newHash,
                    UserId = existing.UserId,
                    ExpiresAt = newExpiry,
                    CreatedAt = DateTime.UtcNow,
                    CreatedByIp = originIp
                });

                await _session.SaveChangesAsync();
            });

            var access = _jwt.GenerateAccessToken(user, roleNames);

            return BaseResult<AuthDto.AuthResponseDto>.Ok(
                new AuthDto.AuthResponseDto(access, newPlain, user.Email, newExpiry));
        }

        public async Task<BaseResult<string>> RegisterAsync(AuthDto.RegisterDto dto, string originIp)
        {
            var exist = await _users.GetFirstOrDefaultAsync(x => x.Email == dto.Email);
            if (exist != null)
                return BaseResult<string>.Fail(AuthErrors.EmailExists);

            await _session.ExecuteTransactionAsync(async () =>
            {
                var user = new User
                {
                    Email = dto.Email,
                    PasswordHash = _hashing.HashPassword(dto.Password),
                    EmailConfirmed = false,
                    Profile = new UserProfile
                    {
                        FullName = dto.Email
                    }
                };

                await _users.AddAsync(user);

                var customerRole = await _roles.GetFirstOrDefaultAsync(r => r.Name == "Customer");
                if (customerRole != null)
                {
                    await _userRoles.AddAsync(new UserRole
                    {
                        UserId = user.Id,
                        RoleId = customerRole.Id
                    });
                }

                await _session.SaveChangesAsync();
            });

            return BaseResult<string>.Ok("Registered successfully.");
        }

        public async Task<BaseResult<string>> CreateUserByAdminAsync(AuthDto.CreateUserByAdminDto dto)
        {
            var existUser = await _users.GetFirstOrDefaultAsync(x => x.Email == dto.Email);
            if (existUser != null)
                return BaseResult<string>.Fail(AuthErrors.EmailExists);

            var role = await _roles.GetFirstOrDefaultAsync(r => r.Name == dto.RoleName);
            if (role == null)
                return BaseResult<string>.Fail("Auth.RoleNotFound", $"Role '{dto.RoleName}' does not exist.", ErrorType.NotFound);

            await _session.ExecuteTransactionAsync(async () =>
            {
                var user = new User
                {
                    Email = dto.Email,
                    PasswordHash = _hashing.HashPassword(dto.Password),
                    EmailConfirmed = true,
                    IsActive = true
                };

                await _users.AddAsync(user);

                await _userRoles.AddAsync(new UserRole
                {
                    UserId = user.Id,
                    RoleId = role.Id
                });

                await _session.SaveChangesAsync();
            });

            return BaseResult<string>.Ok($"User {dto.Email} created successfully with role {dto.RoleName}.");
        }

        public async Task<BaseResult<string>> ResetPasswordAsync(AuthDto.ResetPasswordDto dto)
        {
            if (dto == null || dto.UserId == Guid.Empty ||
                string.IsNullOrWhiteSpace(dto.Token) ||
                string.IsNullOrWhiteSpace(dto.NewPassword))
                return BaseResult<string>.Fail("Auth.InvalidRequest", "Invalid input", ErrorType.Validation);

            var tokenHash = _hashing.HashToken(dto.Token);
            var resetToken = await _passwordResetTokens.GetFirstOrDefaultAsync(
                x => x.UserId == dto.UserId && x.TokenHash == tokenHash);

            if (resetToken == null || resetToken.ExpiresAt < DateTime.UtcNow || resetToken.Used)
                return BaseResult<string>.Fail("Auth.InvalidToken", "Invalid or expired reset token", ErrorType.Validation);

            await _session.ExecuteTransactionAsync(async () =>
            {
                var user = resetToken.User;
                user.PasswordHash = _hashing.HashPassword(dto.NewPassword);
                _users.Update(user);

                resetToken.Used = true;
                _passwordResetTokens.Update(resetToken);

                await _session.SaveChangesAsync();
            });

            return BaseResult<string>.Ok("Password reset successfully.");
        }
    }
}
