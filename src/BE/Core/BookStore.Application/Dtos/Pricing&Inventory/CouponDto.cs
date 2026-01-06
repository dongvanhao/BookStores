using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.Dtos.Pricing_Inventory
{
    public class CouponDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = null!;
        public decimal Value { get; set; }
        public bool IsPercentage { get; set; }
        public DateTime Expiration { get; set; }
    }

}
