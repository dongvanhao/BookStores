using BookStore.Domain.Entities.Ordering;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookStore.Infrastructure.Data.Configurations.Ordering
{
    public class PaymentTransactionConfiguration : IEntityTypeConfiguration<PaymentTransaction>
    {
        public void Configure(EntityTypeBuilder<PaymentTransaction> builder)
        {
            builder.ToTable("PaymentTransactions", "ordering");

            builder.HasKey(p => p.Id);

            builder.Property(p => p.Provider)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(p => p.TransactionCode)
                .HasMaxLength(100);

            builder.Property(p => p.PaymentMethod)
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(p => p.Amount)
                .HasColumnType("decimal(18,2)");

            builder.Property(p => p.Status)
                .HasConversion<string>()
                .HasMaxLength(50)
                .HasDefaultValue(PaymentStatus.Pending);

            builder.Property(p => p.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");
        }
    }
}
