namespace BookStore.Domain.Entities.Ordering
{
    public enum OrderStatus
    {
        Pending,
        Paid,
        Shipped,
        Completed,
        Cancelled,
        Refunded
    }

    public enum PaymentStatus
    {
        Pending,
        Success,
        Failed,
        Refunded
    }
}
