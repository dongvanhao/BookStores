using BookStore.Application.Dtos.Ordering_Payment.Order;
using BookStore.Domain.Entities.Ordering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.Mappers.Ordering_Payment
{
    public static class OrderMapping
    {
        public static OrderSummaryDto ToSummary(this Order o) => new()
        {
            Id = o.Id,
            OrderNumber = o.OrderNumber,
            Status = o.Status,
            FinalAmount = o.FinalAmount,
            CreatedAt = o.CreatedAt
        };

        public static OrderDetailDto ToDetail(this Order o) => new()
        {
            Id = o.Id,
            OrderNumber = o.OrderNumber,
            Status = o.Status,
            TotalAmount = o.TotalAmount,
            DiscountAmount = o.DiscountAmount,
            FinalAmount = o.FinalAmount,
            CreateAt = o.CreatedAt,
            PaidAt = o.PaidAt,
            Items = o.Items.Select(i => new OrderItemDto
            {
                BookId = i.BookId,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice
            }).ToList()

        };
    }
}
