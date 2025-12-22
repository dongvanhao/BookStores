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
    public class BookRepository : GenericRepository<Book>,IBookRepository
    {
        private readonly AppDbContext _context;
        public BookRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<bool> ExistsByISBNAsync(string isbn)
        {
            return await _dbSet.AnyAsync(x => x.ISBN == isbn);
        }

        public async Task<Book?> GetDetailAsync(Guid id)
        {
            return await _dbSet
                .Include(x => x.Publisher)
                .Include(x => x.BookAuthors).ThenInclude(x => x.Author)
                .Include(x => x.BookCategories).ThenInclude(x => x.Category)
                .Include(x => x.Images)
                .FirstOrDefaultAsync(x => x.Id == id);
        }
        public async Task<IReadOnlyList<Book>> GetPagedAsync(int skip, int take)
        {
            return await _dbSet
                .Include(x => x.Publisher)
                .Include(x => x.BookAuthors).ThenInclude(x => x.Author)
                .Include(x => x.BookCategories).ThenInclude(x => x.Category)
                .OrderByDescending(x => x.PublicationYear)
                .Skip(skip)
                .Take(take)
                .AsNoTracking()
                .ToListAsync();
        }

    }
}
