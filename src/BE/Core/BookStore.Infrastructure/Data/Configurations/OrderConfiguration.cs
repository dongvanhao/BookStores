using BookStore.Domain.Entities;
using BookStore.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookStore.Infrastructure.Data.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Status)
               .IsRequired()
               .HasConversion<string>()   // lưu "Pending", "Confirmed"... thay vì số nguyên — dễ đọc trong DB
               .HasMaxLength(20);

        builder.Property(o => o.TotalAmount)
               .IsRequired()
               .HasColumnType("decimal(18,2)");

        builder.Property(o => o.ShippingAddress)
               .IsRequired()
               .HasMaxLength(500);

        builder.Property(o => o.Note)
               .HasMaxLength(1000);

        builder.Property(o => o.CreatedAt).IsRequired();
        builder.Property(o => o.UpdatedAt).IsRequired();

        builder.HasOne(o => o.User)
               .WithMany(u => u.Orders)
               .HasForeignKey(o => o.UserId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(o => o.Items)
               .WithOne(i => i.Order)
               .HasForeignKey(i => i.OrderId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
