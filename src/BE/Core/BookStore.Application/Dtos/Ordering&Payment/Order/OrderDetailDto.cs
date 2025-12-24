using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.Dtos.Ordering_Payment.Order
{
    public class OrderDetailDto
    {
        public Guid Id { get; set; }
        public string OrderNumber { get; set; } = default!;
        public string Status { get; set; } = default!;
        public decimal TotalAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal FinalAmount { get; set; }

        public DateTime CreateAt { get; set; }
        public DateTime? PaidAt { get; set; }

        public IReadOnlyList<OrderItemDto> Items { get; set; } = [];
    }
}
