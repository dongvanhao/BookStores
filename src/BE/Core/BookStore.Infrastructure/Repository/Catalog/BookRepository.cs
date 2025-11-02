using BookStore.Domain.Entities.Catalog;
using BookStore.Domain.IRepository.Catalog;
using BookStore.Infrastructure.Data;
using BookStore.Infrastructure.Repository.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Infrastructure.Repository.Catalog
{
    public class BookRepository : GenericRepository<Book>, IBookRepository
    {
        public BookRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<Book?> GetBookWithDetailsAsync(Guid id)
        {
            return await _dbSet
            .Include(b => b.Description)
            .FirstOrDefaultAsync(b => b.Id == id);
        }

    }
}
