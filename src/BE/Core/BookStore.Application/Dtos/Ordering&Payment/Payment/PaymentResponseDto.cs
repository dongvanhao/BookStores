using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.Dtos.Ordering_Payment.Payment
{
    public class PaymentResponseDto
    {
        public Guid TransactionId { get; set; }
        public string Status { get; set; } = default!;
        public decimal Amount { get; set; }
        public DateTime? PaidAt { get; set; }
    }
}
