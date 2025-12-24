using BookStore.Application.Dtos.Ordering_Payment.Payment;
using BookStore.Application.Dtos.Ordering_Payment.PaymentRequestDto;
using BookStore.Shared.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.IService.Ordering_Payment
{
    public interface IPaymentService
    {
        Task<BaseResult<PaymentResponseDto>> PayAsync(Guid userId, PaymentRequestDto request);
        Task<BaseResult<PaymentResponseDto>> GetByOrderAsync(Guid userId, Guid orderId);
    }
}
