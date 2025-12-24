using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.Services.Ordering_Payment
{
    public class CheckoutResponseDto
    {
        public Guid OrderId { get; set; }
        public string OrderNumber { get; set; } = default!;
        public decimal FinalAmount { get; set; }
    }
}
