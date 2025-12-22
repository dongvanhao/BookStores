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
    public class BookImageRepository : GenericRepository<BookImage>, IBookImageRepository
    {
        public BookImageRepository(AppDbContext context) : base(context) { }

        public async Task<IReadOnlyList<BookImage>> GetByBookIdAsync(Guid bookId)
        {
            return await _dbSet
                .Where(bi => bi.BookId == bookId)
                .OrderBy(x => x.DisplayOrder)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<BookImage?> GetCoverAsync(Guid bookId)
        {
            return await _dbSet
                .FirstOrDefaultAsync(x => x.BookId == bookId && x.IsCover);
        }

    }
}
