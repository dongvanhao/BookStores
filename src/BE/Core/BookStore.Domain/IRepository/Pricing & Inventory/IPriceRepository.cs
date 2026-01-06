using BookStore.Domain.Entities.Pricing_Inventory;
using BookStore.Domain.IRepository.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Domain.IRepository.Pricing___Inventory
{
    public interface IPriceRepository : IGenericRepository<Price>
    {
        Task<Price?> GetCurrentByBookAsync(Guid bookId);
        Task<IReadOnlyList<Price>> GetHistoryByBookAsync(Guid bookId);
    }
}
