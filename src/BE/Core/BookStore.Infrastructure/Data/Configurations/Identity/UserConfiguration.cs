using BookStore.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookStore.Infrastructure.Data.Configurations.Identity
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("Users", "identity");

            //  Khóa chính
            builder.HasKey(u => u.Id);

            //  Email là unique
            builder.HasIndex(u => u.Email).IsUnique();

            // Thuộc tính
            builder.Property(u => u.Email)
                .IsRequired()
                .HasMaxLength(255);

            builder.Property(u => u.PasswordHash)
                .IsRequired();

            builder.Property(u => u.IsActive)
                .HasDefaultValue(true);

            builder.Property(u => u.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            builder.Property(u => u.UpdatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            builder.Property(u => u.EmailConfirmed)
                .HasDefaultValue(false)
                .IsRequired();

            builder.Property(u => u.IsLocked)
                .HasDefaultValue(false)
                .IsRequired();

            builder.HasOne(u => u.Profile)
                .WithOne(p => p.User)
                .HasForeignKey<UserProfile>(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(u => u.Addresses)
                .WithOne(a => a.User)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            //  1-n với RefreshToken
            builder.HasMany(u => u.RefreshTokens)
                .WithOne(t => t.User)
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            //  1-n với UserRole
            builder.HasMany(u => u.UserRoles)
                .WithOne(ur => ur.User)
                .HasForeignKey(ur => ur.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            //  1-n với PasswordResetToken
            builder.HasMany(u => u.PasswordResetTokens)
                .WithOne(t => t.User)
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            //  1-n với EmailVerificationToken
            builder.HasMany(u => u.EmailVerificationTokens)
                .WithOne(t => t.User)
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
