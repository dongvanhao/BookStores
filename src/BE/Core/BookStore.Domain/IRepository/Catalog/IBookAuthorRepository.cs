using BookStore.Domain.Entities.Catalog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Domain.IRepository.Catalog
{
    public interface IBookAuthorRepository
    {
        Task<bool> ExistsAsync(Guid bookId, Guid authorId);
        Task AddAsync(BookAuthor entity);
        Task RemoveAsync(BookAuthor entity);
        Task<IReadOnlyList<BookAuthor>> GetByBookIdAsync(Guid bookId);
    }
}
