using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.Dtos.Ordering_Payment.Order
{
    public class OrderStatusLogDto
    {
        public string OldStatus { get; set; } = default!;
        public string NewStatus { get; set; } = default!;
        public DateTime ChangedAt { get; set; }
        public string? ChangedBy { get; set; }
    }

}
