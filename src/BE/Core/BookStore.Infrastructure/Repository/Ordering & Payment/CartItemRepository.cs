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
    public class CartItemRepository : GenericRepository<CartItem>, ICartItemRepository
    {
        public CartItemRepository(AppDbContext dbContext) : base(dbContext)
        {
        }
        public async Task<IReadOnlyList<CartItem>> GetByCartIdAsync(Guid cartId)
        {
            return await _context.Set<CartItem>()
                .Include(x => x.Book)
                .Where(x => x.CartId == cartId)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<CartItem?> GetByCartAndBookAsync(Guid cartId, Guid bookId)
        {
            return await _context.Set<CartItem>()
                .FirstOrDefaultAsync(x =>
                    x.CartId == cartId && x.BookId == bookId);
        }
    }
}
