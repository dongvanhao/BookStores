using BookStore.Domain.Entities.Pricing_Inventory;
using BookStore.Domain.IRepository.Pricing___Inventory;
using BookStore.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Infrastructure.Repository.Pricing___Inventory
{
    public class StockItemRepository : IStockItemRepository
    {
        private readonly AppDbContext _context;

        public StockItemRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<StockItem?> GetByBookAsync(Guid bookId)
        {
            return await _context.StockItems
                .FirstOrDefaultAsync(x => x.BookId == bookId);
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
