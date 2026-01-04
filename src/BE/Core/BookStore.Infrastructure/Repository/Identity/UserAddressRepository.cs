using BookStore.Domain.Entities.Identity;
using BookStore.Infrastructure.Repository.Common;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Infrastructure.Repository.Identity
{
    public class UserAddressRepository : GenericRepository<Domain.Entities.Identity.UserAddress>, Domain.IRepository.Identity.IUserAddressRepository
    {
        public UserAddressRepository(Data.AppDbContext context) : base(context)
        {
        }
        public async Task<IReadOnlyList<UserAddress>> GetByUserAsync(Guid userId)
        {
            return await _context.UserAddresses
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .ToListAsync();
        }

        public async Task<UserAddress?> GetDefaultAsync(Guid userId)
        {
            return await _context.UserAddresses
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.UserId == userId && x.IsDefault);
        }
    }
}
