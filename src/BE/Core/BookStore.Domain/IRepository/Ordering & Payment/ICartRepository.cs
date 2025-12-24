using BookStore.Domain.Entities.Ordering;
using BookStore.Domain.IRepository.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Domain.IRepository.Ordering___Payment
{
    public interface ICartRepository : IGenericRepository<Cart>
    {
        Task<Cart?> GetActiveByUserAsync(Guid userId);
    }
}
