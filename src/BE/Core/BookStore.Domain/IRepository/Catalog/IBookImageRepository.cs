using BookStore.Domain.Entities.Catalog;
using BookStore.Domain.IRepository.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Domain.IRepository.Catalog
{
    public interface IBookImageRepository : IGenericRepository<Entities.Catalog.BookImage>
    {
        Task<IReadOnlyList<Entities.Catalog.BookImage>> GetByBookIdAsync(Guid bookId);
        Task<BookImage?> GetCoverAsync(Guid bookId);
    }
}
