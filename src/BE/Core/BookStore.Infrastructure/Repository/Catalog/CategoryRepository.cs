using BookStore.Domain.IRepository.Catalog;
using BookStore.Infrastructure.Data;
using BookStore.Infrastructure.Repository.Common;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Infrastructure.Repository.Catalog
{
    public class CategoryRepository : GenericRepository<Domain.Entities.Catalog.Category>, ICategoryRepository
    {
        public CategoryRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<bool> ExistsByNameAsync(string name, Guid? parentId)
        {
            return await _dbSet.AnyAsync(c => c.Name == name && c.ParentId == parentId);
        }

        public async Task<IReadOnlyList<Domain.Entities.Catalog.Category>> GetRootAsync()
        {
            return await _dbSet
                .Where(c => c.ParentId == null)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<IReadOnlyList<Domain.Entities.Catalog.Category>> GetChildrenAsync(Guid parentId)
        {
            return await _dbSet
                .Where(c => c.ParentId == parentId)
                .AsNoTracking()
                .ToListAsync();
        }
    }
}
