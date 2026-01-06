using BookStore.Domain.Entities.Ordering;
using BookStore.Domain.IRepository.Ordering_Payment;
using BookStore.Infrastructure.Data;
using BookStore.Infrastructure.Repository.Common;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Infrastructure.Repository.Ordering_Payment
{
    public class OrderItemRepository : GenericRepository<OrderItem>, IOrderItemRepository
    {
        public OrderItemRepository(AppDbContext context) : base(context) { }

        public async Task<IReadOnlyList<OrderItem>> GetByOrderIdAsync(Guid orderId)
        {
            return await _context.Set<OrderItem>()
                .Include(x => x.Book)
                .Where(x => x.OrderId == orderId)
                .AsNoTracking()
                .ToListAsync();
        }
    }
}
