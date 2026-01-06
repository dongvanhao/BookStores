using BookStore.Application.IService.Pricing_Inventory;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BookStore.API.Controllers.Pricing_Inventory
{
    [Route("api/[controller]")]
    [ApiController]
    public class PriceController : BaseController
    {
        private readonly IPriceService _service;

        public PriceController(IPriceService service)
        {
            _service = service;
        }

        [HttpGet("current/{bookId}")]
        public async Task<IActionResult> GetCurrent(Guid bookId)
            => FromResult(await _service.GetCurrentAsync(bookId));
    }
}
