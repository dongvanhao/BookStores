using BookStore.Domain.Entities.Catalog;

namespace BookStore.Domain.Entities.Pricing_Inventory
{
    public class InventoryTransaction
    {
        public Guid Id { get; set; }
        public Guid BookId { get; set; }
        public InventoryTransactionType Type { get; set; }
        public int QuantityChange { get; set; }
        public string? ReferenceId { get; set; }
        public string? Note { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual Book Book { get; set; } = null!;
    }

    public enum InventoryTransactionType
    {
        Inbound,
        Outbound,
        Adjustment
    }
}
