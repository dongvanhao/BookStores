using BookStore.Application.Dtos.Ordering_Payment.Cart;
using BookStore.Application.IService.Ordering_Payment;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BookStore.API.Controllers.Ordering_Payment
{
    [Route("api/[controller]")]
    [ApiController]
    public class CartItemController : BaseController
    {
        private readonly ICartItemService _service;

        public CartItemController(ICartItemService service)
        {
            _service = service;
        }
        [HttpGet]
        public async Task<IActionResult> GetItems()
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            return FromResult(await _service.GetItemAsync(userId));
        }

        [HttpPost]
        public async Task<IActionResult> Add(AddCartItemRequestDto request)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            return FromResult(await _service.AddAsync(userId, request));
        }

        [HttpPut("{bookId:guid}")]
        public async Task<IActionResult> Update(Guid bookId, UpdateCartItemRequestDto request)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            return FromResult(await _service.UpdateAsync(userId, bookId, request));
        }

        [HttpDelete("{bookId:guid}")]
        public async Task<IActionResult> Remove(Guid bookId)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            return FromResult(await _service.RemoveAsync(userId, bookId));
        }
    }
}
