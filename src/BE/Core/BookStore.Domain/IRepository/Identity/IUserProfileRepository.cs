using BookStore.Domain.IRepository.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Domain.IRepository.Identity
{
    public interface IUserProfileRepository : IGenericRepository<Entities.Identity.UserProfile>
    {
        Task<Entities.Identity.UserProfile?> GetByUserIdAsync(Guid userId);
    }
}
