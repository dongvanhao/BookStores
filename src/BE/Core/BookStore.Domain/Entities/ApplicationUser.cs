using Microsoft.AspNetCore.Identity;

namespace BookStore.Domain.Entities;

public class ApplicationUser : IdentityUser<Guid>
{
    public string FullName { get; private set; } = string.Empty;
    public string? AvatarUrl { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // Navigation properties
    public ICollection<Order> Orders { get; private set; } = [];
    public ICollection<Review> Reviews { get; private set; } = [];
    public ICollection<RefreshToken> RefreshTokens { get; private set; } = [];

    private ApplicationUser() { }

    public static ApplicationUser Create(string fullName, string email, string userName)
    {
        return new ApplicationUser
        {
            Id        = Guid.NewGuid(),
            FullName  = fullName,
            Email     = email,
            UserName  = userName,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void UpdateProfile(string fullName, string? avatarUrl)
    {
        FullName  = fullName;
        AvatarUrl = avatarUrl;
        UpdatedAt = DateTime.UtcNow;
    }
}
