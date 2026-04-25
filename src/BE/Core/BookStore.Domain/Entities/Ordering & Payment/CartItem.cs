using BookStore.Domain.Entities.Catalog;

namespace BookStore.Domain.Entities.Ordering
{
    public class CartItem
    {
        public Guid Id { get; set; }

        public Guid CartId { get; set; }
        public virtual Cart Cart { get; set; } = null!;

        public Guid BookId { get; set; }
        public virtual Book Book { get; set; } = null!;

        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice => Quantity * UnitPrice;

        public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    }
}
