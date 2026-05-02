using BookStore.Domain.Entities;

namespace BookStore.Domain.IRepository;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken ct = default);
    Task RevokeAllActiveByUserIdAsync(Guid userId, CancellationToken ct = default);
    void Add(RefreshToken token);
}
