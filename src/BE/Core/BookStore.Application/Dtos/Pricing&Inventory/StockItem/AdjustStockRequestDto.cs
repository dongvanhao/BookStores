namespace BookStore.Application.Dtos.Pricing_Inventory.StockItem
{
    public class AdjustStockRequestDto
    {
        public Guid BookId { get; set; }
        public int Quantity { get; set; }
        public string? Note { get; set; }
    }
}
