using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.Dtos.Pricing_Inventory
{
    public class CreateCouponDto
    {
        public string Code { get; set; } = null!;
        public decimal Value { get; set; }
        public bool IsPercentage { get; set; }
        public DateTime Expiration { get; set; }
        public Guid? UserId { get; set; }
    }

}
