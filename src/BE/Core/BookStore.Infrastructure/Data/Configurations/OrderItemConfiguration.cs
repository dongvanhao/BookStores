using BookStore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookStore.Infrastructure.Data.Configurations;

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("OrderItems");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.BookTitle)
               .IsRequired()
               .HasMaxLength(300);

        builder.Property(i => i.Quantity)
               .IsRequired();

        builder.Property(i => i.UnitPrice)
               .IsRequired()
               .HasColumnType("decimal(18,2)");

        // SubTotal là computed property — không map vào DB
        builder.Ignore(i => i.SubTotal);

        builder.Property(i => i.CreatedAt).IsRequired();
        builder.Property(i => i.UpdatedAt).IsRequired();

        // FK tới Order đã cấu hình phía Order, chỉ cần FK tới Book ở đây
        builder.HasOne(i => i.Book)
               .WithMany(b => b.OrderItems)
               .HasForeignKey(i => i.BookId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
