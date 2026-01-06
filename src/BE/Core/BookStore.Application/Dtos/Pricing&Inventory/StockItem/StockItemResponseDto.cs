using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.Dtos.Pricing_Inventory.StockItem
{
    public class StockItemResponseDto
    {
        public Guid BookId { get; set; }
        public Guid WarehouseId { get; set; }
        public int QuantityOnHand { get; set; }
        public int ReservedQuantity { get; set; }
        public int SoldQuantity { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}
