using BookStore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookStore.Infrastructure.Data.Configurations;

public class BookConfiguration : IEntityTypeConfiguration<Book>
{
    public void Configure(EntityTypeBuilder<Book> builder)
    {
        builder.ToTable("Books");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.Title)
               .IsRequired()
               .HasMaxLength(300);

        builder.HasIndex(b => b.Title)
               .IsUnique();

        builder.Property(b => b.ISBN)
               .IsRequired()
               .HasMaxLength(20);

        builder.HasIndex(b => b.ISBN)
               .IsUnique();

        builder.Property(b => b.Description)
               .HasMaxLength(5000);

        builder.Property(b => b.CoverUrl)
               .HasMaxLength(500);

        builder.Property(b => b.Price)
               .IsRequired()
               .HasColumnType("decimal(18,2)");

        builder.Property(b => b.StockQuantity)
               .IsRequired();

        builder.Property(b => b.PublishedYear)
               .IsRequired();

        builder.Property(b => b.IsDeleted)
               .IsRequired()
               .HasDefaultValue(false);

        builder.Property(b => b.CreatedAt).IsRequired();
        builder.Property(b => b.UpdatedAt).IsRequired();

        builder.HasOne(b => b.Category)
               .WithMany(c => c.Books)
               .HasForeignKey(b => b.CategoryId)
               .OnDelete(DeleteBehavior.Restrict);

        // Soft delete filter — mặc định không trả về sách đã xoá
        builder.HasQueryFilter(b => !b.IsDeleted);
    }
}
