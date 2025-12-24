using BookStore.Application.Dtos.Ordering_Payment.PaymentRequestDto;
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
    public class PaymentController : BaseController
    {
        private readonly IPaymentService _service;

        public PaymentController(IPaymentService service)
        {
            _service = service;
        }

        [HttpPost("pay")]
        public async Task<IActionResult> Pay(PaymentRequestDto request)
        {
            var userId = Guid.Parse(
                User.FindFirstValue(ClaimTypes.NameIdentifier)!
            );

            return FromResult(await _service.PayAsync(userId, request));
        }

        [HttpGet("order/{orderId:guid}")]
        public async Task<IActionResult> GetByOrder(Guid orderId)
        {
            var userId = Guid.Parse(
                User.FindFirstValue(ClaimTypes.NameIdentifier)!
            );

            return FromResult(await _service.GetByOrderAsync(userId, orderId));
        }
    }
}
