using BookStore.Application.Dtos.Pricing_Inventory.Discount;
using BookStore.Application.IService.Pricing_Inventory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BookStore.API.Controllers.Pricing_Inventory
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminDiscountController : BaseController
    {
        private readonly IDiscountService _service;

        public AdminDiscountController(IDiscountService service)
        {
            _service = service;
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Create(CreateDiscountDto dto)
            => FromResult(await _service.CreateAsync(dto));

        [HttpPut("{id}/toggle")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Toggle(Guid id)
            => FromResult(await _service.ToggleAsync(id));
    }
}
