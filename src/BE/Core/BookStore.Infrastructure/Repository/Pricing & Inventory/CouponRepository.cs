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
    public class CouponRepository : GenericRepository<Coupon>, ICouponRepository
    {
        public CouponRepository(AppDbContext context) : base(context) { }

        public async Task<Coupon?> GetValidByCodeAsync(string code, Guid userId)
        {
            return await _context.Coupons.FirstOrDefaultAsync(c =>
                c.Code == code &&
                !c.IsUsed &&
                c.Expiration > DateTime.UtcNow &&
                (c.UserId == null || c.UserId == userId)
            );
        }

        public async Task<IReadOnlyList<Coupon>> GetAvailableByUserAsync(Guid userId)
        {
            return await _context.Coupons
                .AsNoTracking()
                .Where(c =>
                    !c.IsUsed &&
                    c.Expiration > DateTime.UtcNow &&
                    (c.UserId == null || c.UserId == userId))
                .ToListAsync();
        }
    }
}
