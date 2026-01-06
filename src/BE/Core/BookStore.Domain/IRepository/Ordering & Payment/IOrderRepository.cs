using BookStore.Domain.Entities.Ordering;
using BookStore.Domain.IRepository.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Domain.IRepository.Ordering_Payment
{
    public interface IOrderRepository : IGenericRepository<Order>
    {
        Task<IReadOnlyList<Order>> GetByUserAsync(Guid userId);
        Task<Order?> GetDetailAsync(Guid orderId, Guid userId);
    }
}
