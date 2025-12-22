using BookStore.Domain.Entities.Catalog;
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
    public class BookMetadataRepository : GenericRepository<BookMetadata>, IBookMetadataRepository
    {
        public BookMetadataRepository(AppDbContext context) : base(context) { }

        public async Task<IReadOnlyList<BookMetadata>> GetByBookIdAsync(Guid bookId)
        {
            return await _dbSet
                .Where(x => x.BookId == bookId)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<bool> ExistsKeyAsync(Guid bookId, string key)
        {
            return await _dbSet.AnyAsync(x => x.BookId == bookId && x.Key == key);
        }
    }
}
