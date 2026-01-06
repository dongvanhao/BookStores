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
    public class CartRepository : GenericRepository<Cart>, ICartRepository
    {
        public CartRepository(AppDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<Cart?> GetActiveByUserAsync(Guid userId)
        {
            return await _context.Carts
                .Include(x => x.Items)
                .FirstOrDefaultAsync(x => x.UserId == userId && x.IsActive);
        }
    }
}
