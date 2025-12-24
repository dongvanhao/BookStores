using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.Dtos.Ordering_Payment.Order
{
    public class OrderHistoryDto
    {
        public string Action { get; set; } = default!;
        public string? Details { get; set; }
        public DateTime CreatedAt { get; set; }
    }

}
