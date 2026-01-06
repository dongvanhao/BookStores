using BookStore.Domain.Entities.Pricing_Inventory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.Dtos.Pricing_Inventory.InventoryTransaction
{
    public class InventoryTransactionDto
    {
        public Guid Id { get; set; }
        public Guid BookId { get; set; }
        public InventoryTransactionType Type { get; set; }
        public int QuantityChange { get; set; }
        public string? ReferenceId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
