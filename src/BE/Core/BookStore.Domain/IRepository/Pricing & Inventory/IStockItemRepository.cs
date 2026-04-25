using BookStore.Domain.Entities.Pricing_Inventory;

namespace BookStore.Domain.IRepository.Pricing___Inventory
{
    public interface IStockItemRepository
    {
        Task<StockItem?> GetByBookAsync(Guid bookId);
        Task AddAsync(StockItem stock);
        void Update(StockItem stock);
    }
}
