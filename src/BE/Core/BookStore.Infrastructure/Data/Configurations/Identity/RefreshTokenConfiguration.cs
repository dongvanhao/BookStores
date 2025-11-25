using BookStore.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookStore.Infrastructure.Configurations.Identity
{
    public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
    {
        public void Configure(EntityTypeBuilder<RefreshToken> builder)
        {
            // Table name
            builder.ToTable("RefreshTokens");

            // Primary key
            builder.HasKey(rt => rt.Id);

            // TokenHash (unique, required)
            builder.Property(rt => rt.TokenHash)
                .IsRequired()
                .HasMaxLength(256);

            builder.HasIndex(rt => rt.TokenHash)
                .IsUnique();

            // ReplacedByTokenHash (nullable)
            builder.Property(rt => rt.ReplacedByTokenHash)
                .HasMaxLength(256);

            // CreatedByIp (optional, limit length)
            builder.Property(rt => rt.CreatedByIp)
                .HasMaxLength(45); // enough for IPv6

            // Dates
            builder.Property(rt => rt.CreatedAt)
                .IsRequired();

            builder.Property(rt => rt.ExpiresAt)
                .IsRequired();

            builder.Property(rt => rt.Revoked)
                .HasDefaultValue(false);

            // Relationship with User
            builder.HasOne(rt => rt.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(rt => rt.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
