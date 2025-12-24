using BookStore.Application.Dtos.Ordering_Payment.Order;
using BookStore.Application.Services.Ordering_Payment;
using BookStore.Shared.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.IService.Ordering_Payment
{
    public interface IOrderService
    {
        Task<BaseResult<IReadOnlyList<OrderSummaryDto>>> GetMyOrderAsync(Guid userId);
        Task<BaseResult<OrderDetailDto>> GetDetailAsync(Guid userId, Guid orderId);
        Task<BaseResult<bool>> CancelAsync(Guid userId, Guid orderId);
        Task<BaseResult<CheckoutResponseDto>> CheckoutAsync(Guid userId,CheckoutRequestDto request);
    }
}
