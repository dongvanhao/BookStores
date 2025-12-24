using BookStore.Application.Dtos.Ordering_Payment.Order;
using BookStore.Domain.Entities.Ordering___Payment;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.Mappers.Ordering_Payment
{
    public static class OrderHistoryMapping
    {
        public static OrderHistoryDto ToDto(this OrderHistory history)
        {
            return new OrderHistoryDto
            {
                Action = history.Action,
                Details = history.Details,
                CreatedAt = history.CreatedAt
            };
        }
    }
}
