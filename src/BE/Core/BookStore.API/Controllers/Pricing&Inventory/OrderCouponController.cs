using BookStore.Application.Dtos.Pricing_Inventory;
using BookStore.Application.IService.Pricing_Inventory;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BookStore.API.Controllers.Pricing_Inventory
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderCouponController : BaseController
    {
        private readonly ICouponService _service;

        public OrderCouponController(ICouponService service)
        {
            _service = service;
        }

        [HttpPost]
        public async Task<IActionResult> Apply(ApplyCouponRequestDto dto)
        {
            var userId = Guid.Parse(
                User.FindFirstValue(ClaimTypes.NameIdentifier)!
            );

            return FromResult(await _service.ApplyAsync(userId, dto));
        }
    }
}
