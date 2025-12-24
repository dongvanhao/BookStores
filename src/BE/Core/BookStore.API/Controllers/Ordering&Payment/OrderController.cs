using BookStore.Application.IService.Ordering_Payment;
using BookStore.Application.Services.Ordering_Payment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BookStore.API.Controllers.Ordering_Payment
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderController : BaseController
    {
        private readonly IOrderService _service;

        public OrderController(IOrderService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetMyOrders()
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            return FromResult(await _service.GetMyOrderAsync(userId));
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetDetail(Guid id)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            return FromResult(await _service.GetDetailAsync(userId, id));
        }

        [HttpPost("{id:guid}/cancel")]
        public async Task<IActionResult> Cancel(Guid id)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            return FromResult(await _service.CancelAsync(userId, id));
        }
        [Authorize]
        [HttpPost("checkout")]
        public async Task<IActionResult> Checkout(CheckoutRequestDto request)
        {
            var userId = Guid.Parse(
                User.FindFirstValue(ClaimTypes.NameIdentifier)!
            );

            var result = await _service.CheckoutAsync(userId, request);
            return FromResult(result);
        }

    }
}
