using BookStore.Domain.Entities.Identity;
using BookStore.Domain.IRepository.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Domain.IRepository.Identity
{
    public interface IUserDeviceRepository : IGenericRepository<Entities.Identity.UserDevice>
    {
        Task<IReadOnlyList<UserDevice>> GetByUserAsync(Guid userId);
        Task<UserDevice?> GetByUserAndDeviceAsync(
            Guid userId,
            string deviceName,
            string lastLoginIp
        );
    }
}
