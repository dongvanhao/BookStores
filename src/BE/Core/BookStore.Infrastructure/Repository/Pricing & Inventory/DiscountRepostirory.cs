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
    public class DiscountRepository
       : GenericRepository<Discount>, IDiscountRepository
    {
        public DiscountRepository(AppDbContext context) : base(context) { }

        public async Task<IReadOnlyList<Discount>> GetActiveAsync()
        {
            var now = DateTime.UtcNow;
            return await _context.Discounts
                .AsNoTracking()
                .Where(d =>
                    d.IsActive &&
                    d.StartDate <= now &&
                    d.EndDate >= now)
                .ToListAsync();
        }
    }
}
