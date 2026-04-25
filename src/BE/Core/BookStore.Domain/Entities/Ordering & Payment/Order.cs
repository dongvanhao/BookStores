using BookStore.Domain.Entities.Identity;

namespace BookStore.Domain.Entities.Ordering
{
    public class Order
    {
        public Guid Id { get; set; }
        public string OrderNumber { get; set; } = null!;

        public Guid UserId { get; set; }
        public virtual User User { get; set; } = null!;

        public OrderStatus Status { get; set; } = OrderStatus.Pending;
        public decimal SubTotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal FinalAmount => SubTotal - DiscountAmount;
        public string? CouponCode { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? PaidAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? CancelledAt { get; set; }

        public virtual ICollection<OrderItem> Items { get; set; } = [];
        public virtual OrderAddress ShippingAddress { get; set; } = null!;
        public virtual PaymentTransaction? Payment { get; set; }
        public virtual ICollection<OrderStatusLog> StatusLogs { get; set; } = [];
    }
}
