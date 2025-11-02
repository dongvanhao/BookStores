using BookStore.Domain.IRepository.Catalog;
using BookStore.Domain.IRepository.Common;
using BookStore.Infrastructure.Data;
using BookStore.Infrastructure.Repository.Catalog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Infrastructure.Repository.Common
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;
        public IBookRepository BookRepository { get; private set; }
        public UnitOfWork(AppDbContext context)
        {
            _context = context;
            BookRepository = new BookRepository(_context);
            // ShipperRepository = new ShipperRepository(_context);
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public void Dispose()
        {
            _context.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
