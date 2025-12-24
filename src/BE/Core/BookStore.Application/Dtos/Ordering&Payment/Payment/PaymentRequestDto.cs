using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.Dtos.Ordering_Payment.PaymentRequestDto
{
    public class PaymentRequestDto
    {
        public Guid OrderId { get; set; }
        public bool IsSuccess { get; set; }
    }
}
