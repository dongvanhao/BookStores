using BookStore.Domain.Common;
using BookStore.Domain.Entities.Catalog;

namespace BookStore.Domain.Entities.Pricing_Inventory
{
    public class StockItem : BaseAuditableEntity
    {
        public Guid BookId { get; private set; }
        public virtual Book Book { get; private set; } = null!;

        public int QuantityOnHand { get; private set; }
        public int ReservedQuantity { get; private set; }
        public int AvailableQuantity => QuantityOnHand - ReservedQuantity;

        public void Restock(int amount)
        {
            if (amount <= 0)
                throw new ArgumentException("Amount must be greater than zero.", nameof(amount));

            QuantityOnHand += amount;
            LastUpdated = DateTime.UtcNow;
        }

        public void Reserve(int amount)
        {
            if (amount <= 0)
                throw new ArgumentException("Amount must be greater than zero.", nameof(amount));
            if (AvailableQuantity < amount)
                throw new InvalidOperationException("Not enough stock to reserve.");

            ReservedQuantity += amount;
            LastUpdated = DateTime.UtcNow;
        }

        public void Release(int amount)
        {
            if (amount <= 0)
                throw new ArgumentException("Amount must be greater than zero.", nameof(amount));
            if (ReservedQuantity < amount)
                throw new InvalidOperationException("Reserved quantity is insufficient.");

            ReservedQuantity -= amount;
            LastUpdated = DateTime.UtcNow;
        }

        public void Sell(int amount)
        {
            if (amount <= 0)
                throw new ArgumentException("Amount must be greater than zero.", nameof(amount));
            if (ReservedQuantity < amount)
                throw new InvalidOperationException("Cannot sell more than reserved quantity.");

            ReservedQuantity -= amount;
            QuantityOnHand -= amount;
            LastUpdated = DateTime.UtcNow;
        }

        public void Return(int amount)
        {
            if (amount <= 0)
                throw new ArgumentException("Amount must be greater than zero.", nameof(amount));

            QuantityOnHand += amount;
            LastUpdated = DateTime.UtcNow;
        }
    }
}
