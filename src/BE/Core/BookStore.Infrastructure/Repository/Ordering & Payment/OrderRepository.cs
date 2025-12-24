using BookStore.Domain.Entities.Ordering;
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
    public class OrderRepository : GenericRepository<Order>, IOrderRepository
    {
        public OrderRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<IReadOnlyList<Order>> GetByUserAsync(Guid userId)
        {
            return await _context.Set<Order>()
                .Include(o => o.Items)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.CreatedAt)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<Order?> GetDetailAsync(Guid orderId, Guid userId)
        {
            return await _context.Set<Order>()
                .Include(o => o.Items)
                .Include(o => o.Address)
                .Include(o => o.PaymentTransaction)
                .Include(o => o.StatusLogs)
                .Include(o => o.Histories)
                .FirstOrDefaultAsync(o =>
                    o.Id == orderId && o.UserId == userId);
        }
    }
}
