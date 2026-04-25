using BookStore.Domain.Entities.Catalog;
using BookStore.Domain.IRepository.Catalog;
using BookStore.Infrastructure.Data;
using BookStore.Infrastructure.Repository.Common;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Infrastructure.Repository.Catalog
{
    public class BookRepository : GenericRepository<Book>, IBookRepository
    {
        public BookRepository(AppDbContext context) : base(context) { }

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

        public async Task<(IReadOnlyList<Book> Items, int Total)> SearchAsync(
            string? keyword, Guid? authorId, Guid? categoryId, int skip, int take)
        {
            var query = _dbSet
                .Include(x => x.Publisher)
                .Include(x => x.BookAuthors).ThenInclude(x => x.Author)
                .Include(x => x.BookCategories).ThenInclude(x => x.Category)
                .Where(x => x.IsAvailable)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
                query = query.Where(x =>
                    x.Title.Contains(keyword) ||
                    (x.Description != null && x.Description.Contains(keyword)) ||
                    x.ISBN.Contains(keyword));

            if (authorId.HasValue)
                query = query.Where(x => x.BookAuthors.Any(ba => ba.AuthorId == authorId.Value));

            if (categoryId.HasValue)
                query = query.Where(x => x.BookCategories.Any(bc => bc.CategoryId == categoryId.Value));

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(x => x.PublicationYear)
                .Skip(skip)
                .Take(take)
                .AsNoTracking()
                .ToListAsync();

            return (items, total);
        }
    }
}
