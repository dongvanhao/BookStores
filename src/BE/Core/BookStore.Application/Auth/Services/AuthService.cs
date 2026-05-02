using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using BookStore.Application.Auth.Commands;
using BookStore.Application.Auth.DTOs;
using BookStore.Application.Auth.IService;
using BookStore.Domain.Entities;
using BookStore.Domain.IRepository;
using BookStore.Shared.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace BookStore.Application.Auth.Services;

public class AuthService(
    UserManager<ApplicationUser> userManager,
    IRefreshTokenRepository refreshTokenRepo,
    IUnitOfWork unitOfWork,
    IOptions<JwtOptions> jwtOptions) : IAuthService
{
    private readonly JwtOptions _jwtOptions = jwtOptions.Value;

    public async Task<Result<AuthResponse>> RegisterAsync(RegisterCommand cmd, CancellationToken ct = default)
    {
        var existingUser = await userManager.FindByEmailAsync(cmd.Email);
        if (existingUser is not null)
            return AuthErrors.EmailAlreadyExists;

        var user = ApplicationUser.Create(cmd.FullName, cmd.Email, cmd.Email);

        var createResult = await userManager.CreateAsync(user, cmd.Password);
        if (!createResult.Succeeded)
            return AuthErrors.RegistrationFailed;

        var roleResult = await userManager.AddToRoleAsync(user, "Customer");
        if (!roleResult.Succeeded)
        {
            await userManager.DeleteAsync(user);
            return AuthErrors.RegistrationFailed;
        }

        var (accessToken, refreshTokenStr, expiresAt) = GenerateTokenPair(user, "Customer");

        var refreshToken = RefreshToken.Create(
            user.Id,
            refreshTokenStr,
            DateTime.UtcNow.AddDays(_jwtOptions.RefreshExpiryDays));

        refreshTokenRepo.Add(refreshToken);
        await unitOfWork.SaveChangesAsync(ct);

        return BuildAuthResponse(accessToken, refreshTokenStr, expiresAt, user, "Customer");
    }

    public async Task<Result<AuthResponse>> LoginAsync(LoginCommand cmd, CancellationToken ct = default)
    {
        var user = await userManager.FindByEmailAsync(cmd.Email);
        if (user is null)
            return AuthErrors.InvalidCredentials;

        var passwordValid = await userManager.CheckPasswordAsync(user, cmd.Password);
        if (!passwordValid)
            return AuthErrors.InvalidCredentials;

        var roles = await userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? "Customer";

        await refreshTokenRepo.RevokeAllActiveByUserIdAsync(user.Id, ct);

        var (accessToken, refreshTokenStr, expiresAt) = GenerateTokenPair(user, role);

        var refreshToken = RefreshToken.Create(
            user.Id,
            refreshTokenStr,
            DateTime.UtcNow.AddDays(_jwtOptions.RefreshExpiryDays));

        refreshTokenRepo.Add(refreshToken);
        await unitOfWork.SaveChangesAsync(ct);

        return BuildAuthResponse(accessToken, refreshTokenStr, expiresAt, user, role);
    }

    public async Task<Result<AuthResponse>> RefreshAsync(RefreshTokenCommand cmd, CancellationToken ct = default)
    {
        var existing = await refreshTokenRepo.GetByTokenAsync(cmd.RefreshToken, ct);
        if (existing is null)
            return AuthErrors.RefreshTokenNotFound;

        if (!existing.IsActive)
            return AuthErrors.InvalidRefreshToken;

        existing.Revoke();

        var user = await userManager.FindByIdAsync(existing.UserId.ToString());
        if (user is null)
            return AuthErrors.UserNotFound;

        var roles = await userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? "Customer";

        var (accessToken, refreshTokenStr, expiresAt) = GenerateTokenPair(user, role);

        var newRefreshToken = RefreshToken.Create(
            user.Id,
            refreshTokenStr,
            DateTime.UtcNow.AddDays(_jwtOptions.RefreshExpiryDays));

        refreshTokenRepo.Add(newRefreshToken);
        await unitOfWork.SaveChangesAsync(ct);

        return BuildAuthResponse(accessToken, refreshTokenStr, expiresAt, user, role);
    }

    public async Task<Result> LogoutAsync(LogoutCommand cmd, CancellationToken ct = default)
    {
        var token = await refreshTokenRepo.GetByTokenAsync(cmd.RefreshToken, ct);
        if (token is null)
            return Result.Failure(AuthErrors.RefreshTokenNotFound);

        if (token.UserId != cmd.UserId)
            return Result.Failure(AuthErrors.RefreshTokenNotFound);

        token.Revoke();
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }

    public async Task<Result<UserDto>> GetCurrentUserAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return AuthErrors.UserNotFound;

        var roles = await userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? "Customer";

        return new UserDto(user.Id, user.Email!, user.FullName, role, user.AvatarUrl);
    }

    // -----------------------------------------------------------------------
    // Private helpers
    // -----------------------------------------------------------------------

    private (string accessToken, string refreshToken, DateTime expiresAt) GenerateTokenPair(
        ApplicationUser user, string role)
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(_jwtOptions.AccessExpiryMinutes);
        var accessToken = GenerateAccessToken(user, role, expiresAt);
        var refreshToken = GenerateRefreshToken();
        return (accessToken, refreshToken, expiresAt);
    }

    private string GenerateAccessToken(ApplicationUser user, string role, DateTime expiresAt)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email!),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Role, role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Key));
        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GenerateRefreshToken()
        => Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

    private static AuthResponse BuildAuthResponse(
        string accessToken, string refreshToken, DateTime expiresAt,
        ApplicationUser user, string role)
        => new(
            accessToken,
            refreshToken,
            expiresAt,
            new UserDto(user.Id, user.Email!, user.FullName, role, user.AvatarUrl));
}
