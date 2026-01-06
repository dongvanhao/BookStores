using BookStore.Domain.Entities.Ordering_Payment;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookStore.Infrastructure.Data.Configurations.Ordering
{
    public class OrderAddressConfiguration : IEntityTypeConfiguration<OrderAddress>
    {
        public void Configure(EntityTypeBuilder<OrderAddress> builder)
        {
            builder.ToTable("OrderAddresses", "ordering");

            builder.HasKey(a => a.Id);
            builder.Property(a => a.OrderId)
                   .IsRequired();
            // 🔗 1–1: Order <-> OrderAddress
            builder.HasOne(a => a.Order)
                   .WithOne(o => o.OrderAddress)
                   .HasForeignKey<OrderAddress>(a => a.OrderId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.Property(a => a.RecipientName)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(a => a.PhoneNumber)
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(a => a.Province)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(a => a.District)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(a => a.Ward)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(a => a.Street)
                .HasMaxLength(255)
                .IsRequired();

            builder.Property(a => a.Note)
                .HasMaxLength(500);
        }
    }
}
