using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.Dtos.Pricing_Inventory
{
    public class ApplyCouponRequestDto
    {
        public Guid OrderId { get; set; }
        public string Code { get; set; } = null!;
    }

}
