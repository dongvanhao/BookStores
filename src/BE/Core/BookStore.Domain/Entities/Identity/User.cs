using BookStore.Domain.Entities.Ordering;

namespace BookStore.Domain.Entities.Identity
{
    public class User
    {
        public Guid Id { get; set; }

        public string Email { get; set; } = null!;
        public string PasswordHash { get; set; } = null!;
        public bool IsActive { get; set; } = true;
        public bool EmailConfirmed { get; set; } = false;
        public bool IsLocked { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public virtual UserProfile? Profile { get; set; }
        public virtual ICollection<UserAddress> Addresses { get; set; } = [];
        public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = [];
        public virtual ICollection<PasswordResetToken> PasswordResetTokens { get; set; } = [];
        public virtual ICollection<EmailVerificationToken> EmailVerificationTokens { get; set; } = [];
        public virtual ICollection<UserRole> UserRoles { get; set; } = [];
        public virtual ICollection<Order> Orders { get; set; } = [];
        public virtual Cart? Cart { get; set; }
    }
}
