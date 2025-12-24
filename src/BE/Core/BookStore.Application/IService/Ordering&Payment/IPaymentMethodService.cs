using BookStore.Application.Dtos.Ordering_Payment.Order;
using BookStore.Shared.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.IService.Ordering_Payment
{
    public interface IPaymentMethodService
    {
        Task<BaseResult<IReadOnlyList<PaymentMethodDto>>> GetActiveAsync();
    }
}
