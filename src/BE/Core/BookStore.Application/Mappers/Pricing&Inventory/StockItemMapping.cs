using BookStore.Application.Dtos.Pricing_Inventory.StockItem;
using BookStore.Domain.Entities.Pricing_Inventory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.Mappers.Pricing_Inventory
{
    public static class StockItemMapping
    {
        public static StockItemResponseDto ToDto(this StockItem stock)
        {
            return new StockItemResponseDto
            {
                BookId = stock.BookId,
                WarehouseId = stock.WarehouseId,
                QuantityOnHand = stock.QuantityOnHand,
                ReservedQuantity = stock.ReservedQuantity,
                SoldQuantity = stock.SoldQuantity,
                LastUpdated = stock.LastUpdated
            };
        }
    }
}
