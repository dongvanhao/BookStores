using BookStore.Application.Auth.IService;
using Microsoft.Extensions.Caching.Memory;

namespace BookStore.API.Services;

public class TokenBlacklistService(IMemoryCache cache) : ITokenBlacklistService
{
    private static string Key(string jti) => $"blacklist:{jti}";

    public Task BlacklistAsync(string jti, TimeSpan remaining, CancellationToken ct = default)
    {
        if (remaining > TimeSpan.Zero)
            cache.Set(Key(jti), true, remaining);
        return Task.CompletedTask;
    }

    public Task<bool> IsBlacklistedAsync(string jti, CancellationToken ct = default)
        => Task.FromResult(cache.TryGetValue(Key(jti), out _));
}
