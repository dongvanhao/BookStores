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
    public class OrderStatusLogController : BaseController
    {
        private readonly IOrderStatusLogService _service;

        public OrderStatusLogController(IOrderStatusLogService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> Get(Guid orderId)
        {
            var userId = Guid.Parse(
                User.FindFirstValue(ClaimTypes.NameIdentifier)!
            );

            return FromResult(await _service.GetByOrderAsync(userId, orderId));
        }
    }
}
