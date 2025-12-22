using BookStore.Domain.IRepository.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Domain.IRepository.Catalog
{
    public interface ICategoryRepository : IGenericRepository<Entities.Catalog.Category>
    {
        Task<bool> ExistsByNameAsync(string name, Guid? parentId);
        Task<IReadOnlyList<Entities.Catalog.Category>> GetRootAsync();
        Task<IReadOnlyList<Entities.Catalog.Category>> GetChildrenAsync(Guid parentId);
    }
}
