namespace BookStore.Application.Auth.IService;

public interface ITokenBlacklistService
{
    Task BlacklistAsync(string jti, TimeSpan remaining, CancellationToken ct = default);
    Task<bool> IsBlacklistedAsync(string jti, CancellationToken ct = default);
}
