using BookStore.Domain.Entities.Catalog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Domain.IRepository.Catalog
{
    public interface IBookCategoryRepository
    {
        Task<bool> ExistsAsync(Guid bookId, Guid categoryId);
        Task AddAsync(BookCategory entity);
        Task RemoveAsync(BookCategory entity);
        Task<IReadOnlyList<BookCategory>> GetByBookIdAsync(Guid bookId);
    }
}
