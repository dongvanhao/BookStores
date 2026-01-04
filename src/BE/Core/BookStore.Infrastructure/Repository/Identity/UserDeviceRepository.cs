using BookStore.Domain.Entities.Identity;
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
    public class UserDeviceRepository
       : GenericRepository<UserDevice>, IUserDeviceRepository
    {
        public UserDeviceRepository(AppDbContext context)
            : base(context) { }

        public async Task<IReadOnlyList<UserDevice>> GetByUserAsync(Guid userId)
        {
            return await _context.UserDevices
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.LastLoginAt)
                .ToListAsync();
        }

        public async Task<UserDevice?> GetByUserAndDeviceAsync(
            Guid userId,
            string deviceName,
            string lastLoginIp)
        {
            return await _context.UserDevices.FirstOrDefaultAsync(x =>
                x.UserId == userId &&
                x.DeviceName == deviceName &&
                x.LastLoginIp == lastLoginIp
            );
        }
    }
}
