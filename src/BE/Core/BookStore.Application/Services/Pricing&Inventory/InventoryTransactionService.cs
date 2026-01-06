using BookStore.Application.IService.Pricing_Inventory;
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
    public class InventoryTransactionService : IInventoryTransactionService
    {
        private readonly IUnitOfWork _uow;

        // Giả định 1 warehouse mặc định 
        private readonly Guid _defaultWarehouseId
            = Guid.Parse("11111111-1111-1111-1111-111111111111");

        public InventoryTransactionService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<BaseResult<bool>> OutboundAsync(
            Guid bookId, int quantity, string referenceId, string? note = null)
        {
            var stock = await _uow.InventoryTransaction
                .GetStockAsync(bookId, _defaultWarehouseId);

            if (stock < quantity)
                return BaseResult<bool>.Fail(
                    "Inventory.OutOfStock",
                    "Không đủ tồn kho",
                    ErrorType.Conflict
                );

            await _uow.InventoryTransaction.AddAsync(new InventoryTransaction
            {
                Id = Guid.NewGuid(),
                BookId = bookId,
                WarehouseId = _defaultWarehouseId,
                Type = InventoryTransactionType.Outbound,
                QuantityChange = -quantity,
                ReferenceId = referenceId,
                Note = note
            });

            await _uow.SaveChangesAsync();
            return BaseResult<bool>.Ok(true);
        }

        public async Task<BaseResult<bool>> InboundAsync(
            Guid bookId, int quantity, string referenceId, string? note = null)
        {
            await _uow.InventoryTransaction.AddAsync(new InventoryTransaction
            {
                Id = Guid.NewGuid(),
                BookId = bookId,
                WarehouseId = _defaultWarehouseId,
                Type = InventoryTransactionType.Inbound,
                QuantityChange = quantity,
                ReferenceId = referenceId,
                Note = note
            });

            await _uow.SaveChangesAsync();
            return BaseResult<bool>.Ok(true);
        }
    }
}
