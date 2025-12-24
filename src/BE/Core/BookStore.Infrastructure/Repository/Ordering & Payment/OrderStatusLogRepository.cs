using BookStore.Domain.Entities.Ordering___Payment;
using BookStore.Domain.IRepository.Ordering___Payment;
using BookStore.Infrastructure.Data;
using BookStore.Infrastructure.Repository.Common;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Infrastructure.Repository.Ordering___Payment
{
    public class OrderStatusLogRepository
       : GenericRepository<OrderStatusLog>, IOrderStatusLogRepository
    {
        public OrderStatusLogRepository(AppDbContext context)
            : base(context) { }

        public async Task<IReadOnlyList<OrderStatusLog>> GetByOrderIdAsync(Guid orderId)
        {
            return await _context.Set<OrderStatusLog>()
                .Where(x => x.OrderId == orderId)
                .OrderBy(x => x.ChangedAt)
                .AsNoTracking()
                .ToListAsync();
        }
    }
}
