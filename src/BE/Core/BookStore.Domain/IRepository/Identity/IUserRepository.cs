using BookStore.Domain.Entities.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Domain.IRepository.Identity
{
    public interface IUserRepository : Common.IGenericRepository<Entities.Identity.User>
    {
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetbyidWithRoleAsync(Guid Userid);
    }
}
