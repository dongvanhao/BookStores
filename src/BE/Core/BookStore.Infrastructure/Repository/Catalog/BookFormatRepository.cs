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
    public class BookFormatRepository :GenericRepository<BookFormat>, IBookFormatRepository
    {
        public BookFormatRepository(AppDbContext context) 
            : base(context)
        {
        }
        public async Task<bool> ExistsByTypeAsync(string formatType)
        {
            return await _dbSet.AnyAsync(x => x.FormatType == formatType);
        }
    }
}
