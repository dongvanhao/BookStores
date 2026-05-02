using BookStore.Application.Auth.Commands;
using BookStore.Application.Auth.DTOs;
using BookStore.Shared.Results;

namespace BookStore.Application.Auth.IService;

public interface IAuthService
{
    Task<Result<AuthResponse>> RegisterAsync(RegisterCommand cmd, CancellationToken ct = default);
    Task<Result<AuthResponse>> LoginAsync(LoginCommand cmd, CancellationToken ct = default);
    Task<Result<AuthResponse>> RefreshAsync(RefreshTokenCommand cmd, CancellationToken ct = default);
    Task<Result>               LogoutAsync(LogoutCommand cmd, CancellationToken ct = default);
    Task<Result<UserDto>>      GetCurrentUserAsync(Guid userId, CancellationToken ct = default);
}
