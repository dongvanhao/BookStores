using BookStore.Domain.Entities.Identity;
using BookStore.Domain.IRepository.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Domain.IRepository.Identity
{
    public interface IUserAddressRepository : IGenericRepository<Entities.Identity.UserAddress>
    {
        Task<IReadOnlyList<UserAddress>> GetByUserAsync(Guid userId);
        Task<UserAddress?> GetDefaultAsync(Guid userId);
    }
}
