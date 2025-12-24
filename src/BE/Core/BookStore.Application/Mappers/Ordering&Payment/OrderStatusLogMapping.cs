using BookStore.Application.Dtos.Ordering_Payment.Order;
using BookStore.Domain.Entities.Ordering___Payment;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.Mappers.Ordering_Payment
{
    public static class OrderStatusLogMapping
    {
        public static OrderStatusLogDto ToDto(this OrderStatusLog log)
        {
            return new OrderStatusLogDto
            {
                OldStatus = log.OldStatus,
                NewStatus = log.NewStatus,
                ChangedAt = log.ChangedAt,
                ChangedBy = log.ChangedBy
            };
        }
    }
}
