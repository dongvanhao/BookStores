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
    public class BookAuthorRepository : IBookAuthorRepository
    {
        private readonly AppDbContext _context;

        public BookAuthorRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<bool> ExistsAsync(Guid bookId, Guid authorId)
        {
            return await _context.BookAuthors
                .AnyAsync(x => x.BookId == bookId && x.AuthorId == authorId);
        }

        public async Task AddAsync(BookAuthor entity)
        {
            await _context.BookAuthors.AddAsync(entity);
        }

        public async Task RemoveAsync(BookAuthor entity)
        {
            _context.BookAuthors.Remove(entity);
            await Task.CompletedTask;
        }

        public async Task<IReadOnlyList<BookAuthor>> GetByBookIdAsync(Guid bookId)
        {
            return await _context.BookAuthors
                .Include(x => x.Author)
                .Where(x => x.BookId == bookId)
                .AsNoTracking()
                .ToListAsync();
        }
    }
}
