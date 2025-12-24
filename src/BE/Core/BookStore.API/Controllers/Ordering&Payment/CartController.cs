using BookStore.Application.IService.Ordering_Payment;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BookStore.API.Controllers.Ordering_Payment
{
    [Route("api/[controller]")]
    [ApiController]
    public class CartController : BaseController
    {
        private readonly ICartService _cartService;
        public CartController(ICartService cartService)
        {
            _cartService = cartService;
        }

        [HttpGet("current")]
        public async Task<IActionResult> GetCurrentCart()
        {
            // sandbox
            var userId = Guid.Parse(
                User.FindFirstValue(ClaimTypes.NameIdentifier)!
                );

            var result = await _cartService.GetCurrentCartAsync(userId);
            return FromResult(result);
        }
    }
    
}
