using BookStore.Domain.Entities.Pricing_Inventory;
using BookStore.Domain.IRepository.Pricing___Inventory;
using BookStore.Infrastructure.Data;
using BookStore.Infrastructure.Repository.Common;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Infrastructure.Repository.Pricing___Inventory
{
    public class InventoryTransactionRepository : GenericRepository<InventoryTransaction>, IInventoryTransactionRepository
    {
        public InventoryTransactionRepository(AppDbContext context)
            : base(context) { }

        public async Task<IReadOnlyList<InventoryTransaction>> GetByBookAsync(Guid bookId)
        {
            return await _context.InventoryTransactions
                .AsNoTracking()
                .Where(x => x.BookId == bookId)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();
        }

        public async Task<int> GetStockAsync(Guid bookId, Guid warehouseId)
        {
            return await _context.InventoryTransactions
                .Where(x => x.BookId == bookId && x.WarehouseId == warehouseId)
                .SumAsync(x => x.QuantityChange);
        }
    }
}
