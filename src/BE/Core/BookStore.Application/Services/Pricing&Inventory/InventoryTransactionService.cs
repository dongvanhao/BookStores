using BookStore.Application.IService.Pricing_Inventory;
using BookStore.Domain.Entities.Pricing_Inventory;
using BookStore.Shared.Common;
using BookStore.Domain.IRepository.Common;
using BookStore.Domain.IRepository.Pricing___Inventory;

namespace BookStore.Application.Services.Pricing_Inventory
{
    public class InventoryTransactionService : IInventoryTransactionService
    {
        private readonly IInventoryTransactionRepository _inventoryTransactions;
        private readonly IDbSession _session;

        public InventoryTransactionService(
            IInventoryTransactionRepository inventoryTransactions,
            IDbSession session)
        {
            _inventoryTransactions = inventoryTransactions;
            _session = session;
        }

        public async Task<BaseResult<bool>> OutboundAsync(
            Guid bookId, int quantity, string referenceId, string? note = null)
        {
            var stock = await _inventoryTransactions.GetStockAsync(bookId);

            if (stock < quantity)
                return BaseResult<bool>.Fail("Inventory.OutOfStock", "Insufficient stock.", ErrorType.Conflict);

            await _inventoryTransactions.AddAsync(new InventoryTransaction
            {
                Id = Guid.NewGuid(),
                BookId = bookId,
                Type = InventoryTransactionType.Outbound,
                QuantityChange = -quantity,
                ReferenceId = referenceId,
                Note = note
            });

            await _session.SaveChangesAsync();
            return BaseResult<bool>.Ok(true);
        }

        public async Task<BaseResult<bool>> InboundAsync(
            Guid bookId, int quantity, string referenceId, string? note = null)
        {
            await _inventoryTransactions.AddAsync(new InventoryTransaction
            {
                Id = Guid.NewGuid(),
                BookId = bookId,
                Type = InventoryTransactionType.Inbound,
                QuantityChange = quantity,
                ReferenceId = referenceId,
                Note = note
            });

            await _session.SaveChangesAsync();
            return BaseResult<bool>.Ok(true);
        }
    }
}
