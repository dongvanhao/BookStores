using BookStore.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookStore.Infrastructure.Data.Configurations.Identity
{
    public class EmailVerificationTokenConfiguration : IEntityTypeConfiguration<EmailVerificationToken>
    {
        public void Configure(EntityTypeBuilder<EmailVerificationToken> builder)
        {
            // Tên bảng
            builder.ToTable("EmailVerificationTokens");

            // Khóa chính
            builder.HasKey(e => e.Id);

            // TokenHash: nên lưu hash, không lưu token gốc để bảo mật
            builder.Property(e => e.TokenHash)
                   .IsRequired()
                   .HasMaxLength(256);

            // Ngày hết hạn
            builder.Property(e => e.ExpiresAt)
                   .IsRequired();

            // Quan hệ 1 User - nhiều Token
            builder.HasOne(e => e.User)
                   .WithMany(u => u.EmailVerificationTokens) // cần thêm ICollection trong class User
                   .HasForeignKey(e => e.UserId)
                   .OnDelete(DeleteBehavior.Cascade);

            // Index tối ưu truy vấn theo UserId + TokenHash
            builder.HasIndex(e => new { e.UserId, e.TokenHash })
                   .HasDatabaseName("IX_EmailVerification_User_Token");
        }
    }
}
