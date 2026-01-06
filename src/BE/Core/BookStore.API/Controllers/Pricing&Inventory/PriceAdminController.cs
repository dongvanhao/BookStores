using BookStore.Application.Dtos.Pricing_Inventory.Price;
using BookStore.Application.IService.Pricing_Inventory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BookStore.API.Controllers.Pricing_Inventory
{
    [Route("api/[controller]")]
    [ApiController]
    public class PriceAdminController : BaseController
    {
        private readonly IPriceService _service;

        public PriceAdminController(IPriceService service)
        {
            _service = service;
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Create(CreatePriceRequestDto dto)
            => FromResult(await _service.CreateAsync(dto));

        [HttpGet("book/{bookId}")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> History(Guid bookId)
            => FromResult(await _service.GetHistoryAsync(bookId));
    }
}
