using BookStore.Domain.IRepository.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Domain.IRepository.Catalog
{
    public interface IBookMetadataRepository : IGenericRepository<Entities.Catalog.BookMetadata>
    {
        Task<IReadOnlyList<BookStore.Domain.Entities.Catalog.BookMetadata>> GetByBookIdAsync(Guid bookId);
        Task<bool> ExistsKeyAsync(Guid bookId, string key);
    }
}
