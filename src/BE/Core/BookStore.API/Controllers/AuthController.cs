using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using BookStore.Application.Auth.Commands;
using BookStore.Application.Auth.DTOs;
using BookStore.Application.Auth.IService;
using BookStore.Shared.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookStore.API.Controller;

/// <summary>Authentication endpoints — register, login, refresh, logout, me.</summary>
[Route("api/auth")]
[ApiController]
public class AuthController : BaseController
{
    private readonly IAuthService _authService;
    private readonly ITokenBlacklistService _blacklist;

    public AuthController(IAuthService authService, ITokenBlacklistService blacklist)
    {
        _authService = authService;
        _blacklist   = blacklist;
    }

    /// <summary>Register a new customer account.</summary>
    /// <response code="201">Account created — returns tokens + user info.</response>
    /// <response code="409">Email already in use.</response>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(RegisterCommand cmd, CancellationToken ct)
    {
        var result = await _authService.RegisterAsync(cmd, ct);
        if (result.IsSuccess)
            return StatusCode(StatusCodes.Status201Created, ApiResponse<AuthResponse>.Ok(result.Value));
        return HandleResult(result);
    }

    /// <summary>Authenticate with email and password.</summary>
    /// <response code="200">Returns access token, refresh token and user info.</response>
    /// <response code="401">Invalid credentials.</response>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(LoginCommand cmd, CancellationToken ct)
    {
        var result = await _authService.LoginAsync(cmd, ct);
        return HandleResult(result);
    }

    /// <summary>Exchange a valid refresh token for a new token pair.</summary>
    /// <response code="200">Returns new access token and refresh token.</response>
    /// <response code="401">Refresh token invalid or expired.</response>
    /// <response code="404">Refresh token not found.</response>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Refresh(RefreshTokenCommand cmd, CancellationToken ct)
    {
        var result = await _authService.RefreshAsync(cmd, ct);
        return HandleResult(result);
    }

    /// <summary>Revoke the caller's refresh token (logout).</summary>
    /// <response code="204">Token revoked — no content.</response>
    /// <response code="404">Token not found.</response>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Logout(LogoutCommand cmd, CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized();

        var result = await _authService.LogoutAsync(cmd with { UserId = userId }, ct);
        if (!result.IsSuccess)
            return HandleResult(result);

        var jti = User.FindFirstValue(JwtRegisteredClaimNames.Jti);
        if (jti is not null && long.TryParse(User.FindFirstValue(JwtRegisteredClaimNames.Exp), out var expUnix))
        {
            var remaining = DateTimeOffset.FromUnixTimeSeconds(expUnix).UtcDateTime - DateTime.UtcNow;
            await _blacklist.BlacklistAsync(jti, remaining, ct);
        }

        return NoContent();
    }

    /// <summary>Return the currently authenticated user's profile.</summary>
    /// <response code="200">Returns UserDto.</response>
    /// <response code="401">Not authenticated.</response>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCurrentUser(CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized();
        var result = await _authService.GetCurrentUserAsync(userId, ct);
        return HandleResult(result);
    }
}
