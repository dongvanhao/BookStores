using BookStore.Domain.Entities.Pricing_Inventory;
using BookStore.Domain.IRepository.Pricing___Inventory;
using BookStore.Infrastructure.Data;
using BookStore.Infrastructure.Repository.Common;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Infrastructure.Repository.Pricing___Inventory
{
    public class PriceRepository : GenericRepository<Price>, IPriceRepository
    {
        public PriceRepository(AppDbContext context) : base(context) { }

        public async Task<Price?> GetCurrentByBookAsync(Guid bookId)
        {
            return await _context.Prices
                .AsNoTracking()
                .Include(x => x.Discount)
                .FirstOrDefaultAsync(x =>
                    x.BookId == bookId && x.IsCurrent);
        }

        public async Task<IReadOnlyList<Price>> GetHistoryByBookAsync(Guid bookId)
        {
            return await _context.Prices
                .AsNoTracking()
                .Where(x => x.BookId == bookId)
                .OrderByDescending(x => x.EffectiveFrom)
                .ToListAsync();
        }
    }
}
