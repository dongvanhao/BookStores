using BookStore.Domain.IRepository.Identity;
using BookStore.Infrastructure.Data;
using BookStore.Infrastructure.Repository.Common;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Infrastructure.Repository.Identity
{
    public class UserProfileRepository : GenericRepository<Domain.Entities.Identity.UserProfile>, IUserProfileRepository
    {
        public UserProfileRepository(AppDbContext context) : base(context)
        {
        }
        public async Task<Domain.Entities.Identity.UserProfile?> GetByUserIdAsync(Guid userId)
        {
            return await _context.UserProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(up => up.UserId == userId);
        }
    }
}
