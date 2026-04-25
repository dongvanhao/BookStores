namespace BookStore.Domain.Entities.Ordering
{
    public class OrderStatusLog
    {
        public Guid Id { get; set; }

        public Guid OrderId { get; set; }
        public virtual Order Order { get; set; } = null!;

        public OrderStatus OldStatus { get; set; }
        public OrderStatus NewStatus { get; set; }
        public string? Note { get; set; }
        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    }
}
