using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.Services.Ordering_Payment
{
    public class CheckoutRequestDto
    {
        public Guid AddressId {  get; set; }
        public Guid? CouponId { get; set; }
    }
}
