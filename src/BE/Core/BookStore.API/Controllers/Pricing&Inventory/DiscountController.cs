using BookStore.Application.IService.Pricing_Inventory;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BookStore.API.Controllers.Pricing_Inventory
{
    [Route("api/[controller]")]
    [ApiController]
    public class DiscountController : BaseController
    {
        private readonly IDiscountService _service;

        public DiscountController(IDiscountService service)
        {
            _service = service;
        }

        [HttpGet("active")]
        public async Task<IActionResult> GetActive()
            => FromResult(await _service.GetActiveAsync());
    }
}
