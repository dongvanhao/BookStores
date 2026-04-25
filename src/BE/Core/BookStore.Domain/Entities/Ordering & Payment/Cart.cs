using BookStore.Domain.Entities.Identity;

namespace BookStore.Domain.Entities.Ordering
{
    public class Cart
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }
        public virtual User User { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public virtual ICollection<CartItem> Items { get; set; } = [];
    }
}
