using BookStore.Domain.Entities.Pricing_Inventory;
using BookStore.Domain.IRepository.Pricing___Inventory;
using BookStore.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Infrastructure.Repository.Pricing___Inventory
{
    public class StockItemRepository : IStockItemRepository
    {
        private readonly AppDbContext _context;

        public StockItemRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<StockItem?> GetAsync(Guid bookId, Guid warehouseId)
        {
            return await _context.StockItems
                .FirstOrDefaultAsync(x =>
                    x.BookId == bookId &&
                    x.WarehouseId == warehouseId);
        }

        public async Task<IReadOnlyList<StockItem>> GetByWarehouseAsync(Guid warehouseId)
        {
            return await _context.StockItems
                .AsNoTracking()
                .Where(x => x.WarehouseId == warehouseId)
                .ToListAsync();
        }

        public async Task AddAsync(StockItem stock)
        {
            await _context.StockItems.AddAsync(stock);
        }

        public void Update(StockItem stock)
        {
            _context.StockItems.Update(stock);
        }
    }
}

