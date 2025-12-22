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
    public class BookFileRepository : GenericRepository<BookFile>, IBookFileRepository
    {
        public BookFileRepository(AppDbContext context) : base(context) { }
        public async Task<IReadOnlyList<BookFile>> GetByBookIdAsync(Guid bookId)
        {
            return await _dbSet
                .Where(bf => bf.BookId == bookId)
                .AsNoTracking()
                .ToListAsync();
        }
    }
}
