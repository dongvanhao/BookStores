using BookStore.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookStore.Infrastructure.Data.Configurations.Identity
{
    public class PasswordResetTokenConfiguration : IEntityTypeConfiguration<PasswordResetToken>
    {
        public void Configure(EntityTypeBuilder<PasswordResetToken> builder)
        {
            builder.ToTable("PasswordResetTokens", "identity");

            builder.HasKey(t => t.Id);

            builder.Property(t => t.TokenHash)
                   .IsRequired()
                   .HasMaxLength(256);

            builder.Property(t => t.ExpiresAt)
                   .IsRequired();

            builder.Property(t => t.Used)
                   .HasDefaultValue(false);

            builder.HasOne(t => t.User)
                   .WithMany(u => u.PasswordResetTokens)
                   .HasForeignKey(t => t.UserId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(t => new { t.UserId, t.TokenHash })
                   .HasDatabaseName("IX_PasswordReset_User_Token");
        }
    }
}
