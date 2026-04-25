using BookStore.Domain.Entities.Pricing_Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookStore.Infrastructure.Configurations.Pricing_Inventory
{
    public class StockItemConfiguration : IEntityTypeConfiguration<StockItem>
    {
        public void Configure(EntityTypeBuilder<StockItem> builder)
        {
            builder.ToTable("StockItems");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.QuantityOnHand)
                   .IsRequired();

            builder.Property(x => x.ReservedQuantity)
                   .IsRequired();

            builder.Ignore(x => x.AvailableQuantity);

            builder.Property(x => x.LastUpdated)
                   .IsRequired();

            builder.HasOne(x => x.Book)
                    .WithOne(b => b.StockItem)
                    .HasForeignKey<StockItem>(x => x.BookId)
                    .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => x.BookId).IsUnique();
        }
    }
}
