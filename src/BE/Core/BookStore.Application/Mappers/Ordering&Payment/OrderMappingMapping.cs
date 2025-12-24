using BookStore.Application.Dtos.Ordering_Payment.Order;
using BookStore.Domain.Entities.Ordering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.Mappers.Ordering_Payment
{
    public static class OrderMappingMapping
    {
        public static OrderItemDto ToDto(this OrderItem item)
        {
            return new OrderItemDto
            {
                BookId = item.BookId,
                BookTitle = item.Book.Title,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                Subtotal = item.Subtotal
            };
        }
    }
}
