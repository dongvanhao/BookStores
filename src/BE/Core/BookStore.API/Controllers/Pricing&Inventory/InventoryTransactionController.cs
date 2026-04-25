using BookStore.Application.Dtos.Pricing_Inventory.InventoryTransaction;
using BookStore.Domain.IRepository.Pricing___Inventory;
using Microsoft.AspNetCore.Mvc;

namespace BookStore.API.Controllers.Pricing_Inventory
{
    [Route("api/[controller]")]
    [ApiController]
    public class InventoryTransactionController : BaseController
    {
        private readonly IInventoryTransactionRepository _inventoryTransactions;

        public InventoryTransactionController(IInventoryTransactionRepository inventoryTransactions)
        {
            _inventoryTransactions = inventoryTransactions;
        }

        [HttpGet("book/{bookId}")]
        public async Task<IActionResult> GetByBook(Guid bookId)
        {
            var list = await _inventoryTransactions.GetByBookAsync(bookId);

            return Ok(list.Select(x => new InventoryTransactionDto
            {
                Id = x.Id,
                BookId = x.BookId,
                Type = x.Type,
                QuantityChange = x.QuantityChange,
                ReferenceId = x.ReferenceId,
                CreatedAt = x.CreatedAt
            }));
        }
    }
}
