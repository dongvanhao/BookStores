using BookStore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookStore.Infrastructure.Data.Configurations;

public class MediaConfiguration : IEntityTypeConfiguration<Media>
{
    public void Configure(EntityTypeBuilder<Media> builder)
    {
        builder.ToTable("Media");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.ObjectKey)
               .IsRequired()
               .HasMaxLength(500);

        builder.HasIndex(m => m.ObjectKey)
               .IsUnique()
               .HasDatabaseName("UX_Media_ObjectKey");

        builder.Property(m => m.ThumbnailKey)
               .HasMaxLength(500);

        builder.Property(m => m.BucketName)
               .IsRequired()
               .HasMaxLength(100);

        builder.Property(m => m.Module)
               .IsRequired()
               .HasMaxLength(50);

        builder.Property(m => m.OriginalFileName)
               .IsRequired()
               .HasMaxLength(255);

        builder.Property(m => m.MimeType)
               .IsRequired()
               .HasMaxLength(100);

        builder.Property(m => m.Size)
               .IsRequired();

        builder.Property(m => m.Type)
               .IsRequired()
               .HasConversion<string>();

        builder.Property(m => m.CreatedAt).IsRequired();
        builder.Property(m => m.UpdatedAt).IsRequired();

        builder.HasOne<ApplicationUser>()
               .WithMany()
               .HasForeignKey(m => m.UploadedBy)
               .OnDelete(DeleteBehavior.Restrict);

        // Index tối ưu cho cursor pagination
        builder.HasIndex(m => new { m.UploadedBy, m.Module, m.CreatedAt })
               .HasDatabaseName("IX_Media_UploadedBy_Module_CreatedAt");
    }
}
