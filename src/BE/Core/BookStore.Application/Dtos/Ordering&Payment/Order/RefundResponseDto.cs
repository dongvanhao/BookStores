using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.Dtos.Ordering_Payment.Order
{
    public class RefundResponseDto
    {
        public Guid RefundId { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; } = default!;
        public DateTime RequestedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
    }

}
