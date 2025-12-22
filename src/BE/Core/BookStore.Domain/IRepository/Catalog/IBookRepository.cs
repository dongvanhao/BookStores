using BookStore.Domain.Entities.Catalog;
using BookStore.Domain.IRepository.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Domain.IRepository.Catalog
{
    public interface IBookRepository : IGenericRepository<Book>
    {
        Task<bool> ExistsByISBNAsync(string isbn);
        Task<Book?> GetDetailAsync(Guid id);
        Task<IReadOnlyList<Book>> GetPagedAsync(int skip, int take);
    }
}
