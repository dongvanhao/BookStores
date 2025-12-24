using BookStore.Domain.Entities.Ordering___Payment;
using BookStore.Domain.IRepository.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Domain.IRepository.Ordering___Payment
{
    public interface IRefundRepository
        : IGenericRepository<Refund>
    {
        Task<IReadOnlyList<Refund>> GetByPaymentIdAsync(Guid paymentTransactionId);
    }
}
