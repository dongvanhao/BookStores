using BookStore.Application.Dtos.Pricing_Inventory.StockItem;
using BookStore.Application.IService.Pricing_Inventory;
using BookStore.Application.Mappers.Pricing_Inventory;
using BookStore.Domain.Entities.Pricing_Inventory;
using BookStore.Domain.IRepository.Common;
using BookStore.Shared.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.Services.Pricing_Inventory
{
    public class StockItemService : IStockItemService
    {
        private readonly IUnitOfWork _uow;

        public StockItemService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<BaseResult<StockItemResponseDto>> GetAsync(
            Guid bookId, Guid warehouseId)
        {
            var stock = await _uow.StockItem.GetAsync(bookId, warehouseId);
            if (stock == null)
                return BaseResult<StockItemResponseDto>.NotFound("Chưa có tồn kho");

            return BaseResult<StockItemResponseDto>.Ok(stock.ToDto());
        }

        public async Task<BaseResult<bool>> IncreaseAsync(
            AdjustStockRequestDto dto)
        {
            var stock = await GetOrCreateAsync(dto.BookId, dto.WarehouseId);

            // Domain logic
            stock.Increase(dto.Quantity);
            _uow.StockItem.Update(stock);

            // Audit
            await _uow.InventoryTransaction.AddAsync(new InventoryTransaction
            {
                Id = Guid.NewGuid(),
                BookId = dto.BookId,
                WarehouseId = dto.WarehouseId,
                Type = InventoryTransactionType.Inbound,
                QuantityChange = dto.Quantity,
                Note = dto.Note
            });

            await _uow.SaveChangesAsync();
            return BaseResult<bool>.Ok(true);
        }

        public async Task<BaseResult<bool>> DecreaseAsync(
            AdjustStockRequestDto dto)
        {
            var stock = await _uow.StockItem.GetAsync(dto.BookId, dto.WarehouseId);
            if (stock == null)
                return BaseResult<bool>.NotFound("Không có tồn kho");

            // Domain logic
            stock.Decrease(dto.Quantity);
            _uow.StockItem.Update(stock);

            // Audit
            await _uow.InventoryTransaction.AddAsync(new InventoryTransaction
            {
                Id = Guid.NewGuid(),
                BookId = dto.BookId,
                WarehouseId = dto.WarehouseId,
                Type = InventoryTransactionType.Adjustment,
                QuantityChange = -dto.Quantity,
                Note = dto.Note
            });

            await _uow.SaveChangesAsync();
            return BaseResult<bool>.Ok(true);
        }

        // -----------------------
        // PRIVATE HELPERS
        // -----------------------
        private async Task<StockItem> GetOrCreateAsync(
            Guid bookId, Guid warehouseId)
        {
            var stock = await _uow.StockItem.GetAsync(bookId, warehouseId);
            if (stock != null) return stock;

            stock = new StockItem
            {
                Id = Guid.NewGuid(),
                BookId = bookId,
                WarehouseId = warehouseId
            };

            await _uow.StockItem.AddAsync(stock);
            return stock;
        }
    }
}
