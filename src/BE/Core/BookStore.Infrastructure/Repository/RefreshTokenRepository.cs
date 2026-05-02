using BookStore.Domain.Entities;
using BookStore.Domain.IRepository;
using BookStore.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Infrastructure.Repository;

public class RefreshTokenRepository(AppDbContext context) : IRefreshTokenRepository
{
    public Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken ct = default)
        => context.RefreshTokens.FirstOrDefaultAsync(r => r.Token == token, ct);

    public async Task RevokeAllActiveByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        var activeTokens = await context.RefreshTokens
            .Where(r => r.UserId == userId && !r.IsRevoked && r.ExpiresAt > DateTime.UtcNow)
            .ToListAsync(ct);

        foreach (var t in activeTokens)
            t.Revoke();
    }

    public void Add(RefreshToken token)
        => context.RefreshTokens.Add(token);
}
