using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.Dtos.Pricing_Inventory.StockItem
{
    public class AdjustStockRequestDto
    {
        public Guid BookId { get; set; }
        public Guid WarehouseId { get; set; }
        public int Quantity { get; set; }
        public string? Note { get; set; }
    }
}
