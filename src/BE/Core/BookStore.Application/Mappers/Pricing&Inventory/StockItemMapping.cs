using BookStore.Application.Dtos.Pricing_Inventory.StockItem;
using BookStore.Domain.Entities.Pricing_Inventory;

namespace BookStore.Application.Mappers.Pricing_Inventory
{
    public static class StockItemMapping
    {
        public static StockItemResponseDto ToDto(this StockItem stock) => new()
        {
            BookId = stock.BookId,
            QuantityOnHand = stock.QuantityOnHand,
            ReservedQuantity = stock.ReservedQuantity,
            AvailableQuantity = stock.AvailableQuantity,
            LastUpdated = stock.LastUpdated
        };
    }
}
