using BookStore.Domain.Entities.Ordering;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookStore.Infrastructure.Data.Configurations.Ordering
{
    public class OrderStatusLogConfiguration : IEntityTypeConfiguration<OrderStatusLog>
    {
        public void Configure(EntityTypeBuilder<OrderStatusLog> builder)
        {
            builder.ToTable("OrderStatusLogs", "ordering");

            builder.HasKey(s => s.Id);

            builder.Property(s => s.OldStatus)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(s => s.NewStatus)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(s => s.Note)
                .HasMaxLength(500);
        }
    }
}
