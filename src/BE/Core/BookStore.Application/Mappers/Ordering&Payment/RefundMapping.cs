using BookStore.Application.Dtos.Ordering_Payment.Order;
using BookStore.Domain.Entities.Ordering___Payment;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.Mappers.Ordering_Payment
{
    public static class RefundMapping
    {
        public static RefundResponseDto ToDto(this Refund refund)
        {
            return new RefundResponseDto
            {
                RefundId = refund.Id,
                Amount = refund.Amount,
                Status = refund.Status,
                RequestedAt = refund.RequestedAt,
                ProcessedAt = refund.ProcessedAt
            };
        }
    }
}
