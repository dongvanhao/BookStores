using BookStore.Domain.Entities.Ordering;
using BookStore.Domain.IRepository.Ordering_Payment;
using BookStore.Infrastructure.Data;
using BookStore.Infrastructure.Repository.Common;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Infrastructure.Repository.Ordering_Payment
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
                .Include(o => o.ShippingAddress)
                .Include(o => o.Payment)
                .Include(o => o.StatusLogs)
                .FirstOrDefaultAsync(o =>
                    o.Id == orderId && o.UserId == userId);
        }
    }
}
