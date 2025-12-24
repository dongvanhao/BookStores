using BookStore.Application.Dtos.Ordering_Payment.Order;
using BookStore.Application.IService.Ordering_Payment;
using BookStore.Application.Mappers.Ordering_Payment;
using BookStore.Domain.IRepository.Common;
using BookStore.Shared.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.Services.Ordering_Payment
{
    public class PaymentMethodService : IPaymentMethodService
    {
        private readonly IUnitOfWork _uow;

        public PaymentMethodService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<BaseResult<IReadOnlyList<PaymentMethodDto>>> GetActiveAsync()
        {
            var methods = await _uow.PaymentMethod.GetActiveAsync();

            return BaseResult<IReadOnlyList<PaymentMethodDto>>.Ok(
                methods.Select(x => x.ToDto()).ToList()
            );
        }
    }
}
