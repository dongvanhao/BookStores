using BookStore.Domain.Entities.Ordering_Payment;
using BookStore.Domain.IRepository.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Domain.IRepository.Ordering_Payment
{
    public interface IOrderHistoryRepository
        : IGenericRepository<OrderHistory>
    {
        Task<IReadOnlyList<OrderHistory>> GetByOrderIdAsync(Guid orderId);
    }
}
