using BookStore.Application.Dtos.Ordering_Payment.Order;
using BookStore.Application.IService.Ordering_Payment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BookStore.API.Controllers.Ordering_Payment
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class RefundController : BaseController
    {
        private readonly IRefundService _service;

        public RefundController(IRefundService service)
        {
            _service = service;
        }

        [HttpPost]
        public async Task<IActionResult> RequestRefund(CreateRefundRequestDto request)
        {
            var userId = Guid.Parse(
                User.FindFirstValue(ClaimTypes.NameIdentifier)!
            );

            return FromResult(await _service.RequestRefundAsync(userId, request));
        }

        [HttpGet("/api/orders/{orderId:guid}/refunds")]
        public async Task<IActionResult> GetByOrder(Guid orderId)
        {
            var userId = Guid.Parse(
                User.FindFirstValue(ClaimTypes.NameIdentifier)!
            );

            return FromResult(await _service.GetByOrderAsync(userId, orderId));
        }
    }
}
