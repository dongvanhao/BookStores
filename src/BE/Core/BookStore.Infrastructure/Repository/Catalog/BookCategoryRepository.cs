using BookStore.Domain.Entities.Catalog;
using BookStore.Domain.IRepository.Catalog;
using BookStore.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Infrastructure.Repository.Catalog
{
    public class BookCategoryRepository : IBookCategoryRepository
    {
        private readonly AppDbContext _context;

        public BookCategoryRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<bool> ExistsAsync(Guid bookId, Guid categoryId)
        {
            return await _context.BookCategories
                .AnyAsync(x => x.BookId == bookId && x.CategoryId == categoryId);
        }

        public async Task AddAsync(BookCategory entity)
        {
            await _context.BookCategories.AddAsync(entity);
        }

        public async Task RemoveAsync(BookCategory entity)
        {
            _context.BookCategories.Remove(entity);
            await Task.CompletedTask;
        }

        public async Task<IReadOnlyList<BookCategory>> GetByBookIdAsync(Guid bookId)
        {
            return await _context.BookCategories
                .Include(x => x.Category)
                .Where(x => x.BookId == bookId)
                .AsNoTracking()
                .ToListAsync();
        }
    }
}
