using BookStore.Application.Dtos.Pricing_Inventory.StockItem;
using BookStore.Application.IService.Pricing_Inventory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookStore.API.Controllers.Pricing_Inventory
{
    [Route("api/[controller]")]
    [ApiController]
    public class StockItemAdminController : BaseController
    {
        private readonly IStockItemService _service;

        public StockItemAdminController(IStockItemService service)
        {
            _service = service;
        }

        [HttpGet]
        [Authorize(Roles = "Admin,InventoryManager")]
        public async Task<IActionResult> Get(Guid bookId)
            => FromResult(await _service.GetAsync(bookId));

        [HttpPost("restock")]
        [Authorize(Roles = "Admin,InventoryManager")]
        public async Task<IActionResult> Restock(AdjustStockRequestDto dto)
            => FromResult(await _service.IncreaseAsync(dto));

        [HttpPost("decrease")]
        [Authorize(Roles = "Admin,InventoryManager")]
        public async Task<IActionResult> Decrease(AdjustStockRequestDto dto)
            => FromResult(await _service.DecreaseAsync(dto));
    }
}
