namespace BookStore.Application.Dtos.Pricing_Inventory.StockItem
{
    public class StockItemResponseDto
    {
        public Guid BookId { get; set; }
        public int QuantityOnHand { get; set; }
        public int ReservedQuantity { get; set; }
        public int AvailableQuantity { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}
