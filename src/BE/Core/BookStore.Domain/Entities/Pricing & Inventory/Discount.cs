namespace BookStore.Domain.Entities.Pricing_Inventory
{
    public class Discount
    {
        public Guid Id { get; set; }

        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public decimal Percentage { get; set; }
        public decimal? MaxAmount { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; } = true;

        public bool IsValid() =>
            IsActive && DateTime.UtcNow >= StartDate && DateTime.UtcNow <= EndDate;

        public decimal Calculate(decimal originalPrice)
        {
            if (!IsValid()) return 0;
            var discounted = originalPrice * Percentage;
            return MaxAmount.HasValue ? Math.Min(discounted, MaxAmount.Value) : discounted;
        }
    }
}
