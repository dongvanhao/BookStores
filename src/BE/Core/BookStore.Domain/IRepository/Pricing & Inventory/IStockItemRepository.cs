using BookStore.Domain.Entities.Pricing_Inventory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Domain.IRepository.Pricing___Inventory
{
    public interface IStockItemRepository
    {
        Task<StockItem?> GetAsync(Guid bookId, Guid warehouseId);
        Task<IReadOnlyList<StockItem>> GetByWarehouseAsync(Guid warehouseId);
        Task AddAsync(StockItem stock);
        void Update(StockItem stock);
    }
}
