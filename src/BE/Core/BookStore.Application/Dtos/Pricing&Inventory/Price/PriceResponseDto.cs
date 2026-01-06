using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.Dtos.Pricing_Inventory.Price
{
    public class PriceResponseDto
    {
        public Guid Id { get; set; }
        public decimal Amount { get; set; }
        public bool IsCurrent { get; set; }
        public DateTime EffectiveFrom { get; set; }
        public Guid? DiscountId { get; set; }
    }
}
