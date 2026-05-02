using BookStore.Domain.Common;

namespace BookStore.Domain.Entities;

public class RefreshToken : BaseEntity
{
    public string Token { get; private set; } = string.Empty;
    public DateTime ExpiresAt { get; private set; }
    public bool IsRevoked { get; private set; }
    public DateTime? RevokedAt { get; private set; }

    // FK
    public Guid UserId { get; private set; }
    public ApplicationUser User { get; private set; } = null!;

    private RefreshToken() { }

    public static RefreshToken Create(Guid userId, string token, DateTime expiresAt)
    {
        return new RefreshToken
        {
            Id        = Guid.NewGuid(),
            UserId    = userId,
            Token     = token,
            ExpiresAt = expiresAt,
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsActive  => !IsRevoked && !IsExpired;

    public void Revoke()
    {
        IsRevoked  = true;
        RevokedAt  = DateTime.UtcNow;
        UpdatedAt  = DateTime.UtcNow;
    }
}
