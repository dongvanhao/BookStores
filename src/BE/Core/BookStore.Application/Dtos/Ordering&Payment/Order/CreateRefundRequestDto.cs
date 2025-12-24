using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.Dtos.Ordering_Payment.Order
{
    public class CreateRefundRequestDto
    {
        public Guid OrderId { get; set; }
        public decimal Amount { get; set; }
        public string Reason { get; set; } = default!;
    }

}
