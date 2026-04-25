using BookStore.Application.Dtos.Pricing_Inventory.StockItem;
using BookStore.Application.IService.Pricing_Inventory;
using BookStore.Application.Mappers.Pricing_Inventory;
using BookStore.Domain.Entities.Pricing_Inventory;
using BookStore.Shared.Common;
using BookStore.Domain.IRepository.Common;
using BookStore.Domain.IRepository.Pricing___Inventory;

namespace BookStore.Application.Services.Pricing_Inventory
{
    public class StockItemService : IStockItemService
    {
        private readonly IInventoryTransactionRepository _inventoryTransactions;
        private readonly IStockItemRepository _stockItems;
        private readonly IDbSession _session;

        public StockItemService(
            IInventoryTransactionRepository inventoryTransactions,
            IStockItemRepository stockItems,
            IDbSession session)
        {
            _inventoryTransactions = inventoryTransactions;
            _stockItems = stockItems;
            _session = session;
        }

        public async Task<BaseResult<StockItemResponseDto>> GetAsync(Guid bookId)
        {
            var stock = await _stockItems.GetByBookAsync(bookId);
            if (stock == null)
                return BaseResult<StockItemResponseDto>.NotFound("No stock found.");

            return BaseResult<StockItemResponseDto>.Ok(stock.ToDto());
        }

        public async Task<BaseResult<bool>> IncreaseAsync(AdjustStockRequestDto dto)
        {
            var stock = await GetOrCreateAsync(dto.BookId);
            stock.Restock(dto.Quantity);
            _stockItems.Update(stock);

            await _inventoryTransactions.AddAsync(new InventoryTransaction
            {
                Id = Guid.NewGuid(),
                BookId = dto.BookId,
                Type = InventoryTransactionType.Inbound,
                QuantityChange = dto.Quantity,
                Note = dto.Note
            });

            await _session.SaveChangesAsync();
            return BaseResult<bool>.Ok(true);
        }

        public async Task<BaseResult<bool>> DecreaseAsync(AdjustStockRequestDto dto)
        {
            var stock = await _stockItems.GetByBookAsync(dto.BookId);
            if (stock == null)
                return BaseResult<bool>.NotFound("No stock found.");

            if (stock.AvailableQuantity < dto.Quantity)
                return BaseResult<bool>.Fail("Stock.Insufficient", "Insufficient stock.", ErrorType.Conflict);

            stock.Reserve(dto.Quantity);
            _stockItems.Update(stock);

            await _inventoryTransactions.AddAsync(new InventoryTransaction
            {
                Id = Guid.NewGuid(),
                BookId = dto.BookId,
                Type = InventoryTransactionType.Adjustment,
                QuantityChange = -dto.Quantity,
                Note = dto.Note
            });

            await _session.SaveChangesAsync();
            return BaseResult<bool>.Ok(true);
        }

        private async Task<StockItem> GetOrCreateAsync(Guid bookId)
        {
            var stock = await _stockItems.GetByBookAsync(bookId);
            if (stock != null) return stock;

            stock = new StockItem { Id = Guid.NewGuid(), BookId = bookId };
            await _stockItems.AddAsync(stock);
            return stock;
        }
    }
}
