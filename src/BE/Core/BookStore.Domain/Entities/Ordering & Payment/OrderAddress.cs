namespace BookStore.Domain.Entities.Ordering
{
    public class OrderAddress
    {
        public Guid Id { get; set; }
        public Guid OrderId { get; set; }
        public string RecipientName { get; set; } = null!;
        public string PhoneNumber { get; set; } = null!;
        public string Province { get; set; } = null!;
        public string District { get; set; } = null!;
        public string Ward { get; set; } = null!;
        public string Street { get; set; } = null!;
        public string? Note { get; set; }

        public virtual Order? Order { get; set; }
    }
}
