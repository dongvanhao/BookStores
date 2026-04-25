using BookStore.Application.Dtos.Ordering_Payment.Cart;
using BookStore.Application.IService.Ordering_Payment;
using BookStore.Application.Mappers.Ordering_Payment;
using BookStore.Domain.Entities.Ordering;
using BookStore.Shared.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BookStore.Domain.IRepository.Common;
using BookStore.Domain.IRepository.Ordering_Payment;

namespace BookStore.Application.Services.Ordering_Payment
{
    public class CartItemService : ICartItemService
    {
        private readonly ICartRepository _carts;
        private readonly ICartItemRepository _cartItems;
        private readonly IDbSession _session;
        public CartItemService(ICartRepository carts, ICartItemRepository cartItems, IDbSession session)
        {
            _carts = carts;
            _cartItems = cartItems;
            _session = session;
        }

        public async Task<BaseResult<IReadOnlyList<CartItemResponseDto>>> GetItemAsync(Guid userId)
        {
            var cart = await _carts.GetActiveByUserAsync(userId);
            if (cart == null)
                return BaseResult<IReadOnlyList<CartItemResponseDto>>.Ok([]);


            var items = await _cartItems.GetByCartIdAsync(cart.Id);
            return BaseResult<IReadOnlyList<CartItemResponseDto>>.Ok(
                items.Select(x => x.ToResponse()).ToList()
                );

        }

        public async Task<BaseResult<IReadOnlyList<CartItemResponseDto>>> AddAsync(
            Guid userId, AddCartItemRequestDto request)
        {
            if (request.Quantity <= 0)
                return BaseResult<IReadOnlyList<CartItemResponseDto>>.Fail(
                    "CartItem.InvalidQuantity",
                    "Quantity must be greater than 0.",
                    ErrorType.Validation
                );

            var cart = await _carts.GetActiveByUserAsync(userId);
            if (cart == null)
                return BaseResult<IReadOnlyList<CartItemResponseDto>>.NotFound("Cart not found.");

            var item = await _cartItems.GetByCartAndBookAsync(cart.Id, request.BookId);

            if (item == null)
            {
                // ⚠️ UnitPrice phải snapshot từ Book (bạn có thể lấy từ repository Book)
                item = new CartItem
                {
                    Id = Guid.NewGuid(),
                    CartId = cart.Id,
                    BookId = request.BookId,
                    Quantity = request.Quantity,
                    UnitPrice = 0 // TODO: lấy giá sách
                };

                await _cartItems.AddAsync(item);
            }
            else
            {
                item.Quantity += request.Quantity;
                _cartItems.Update(item);
            }

            await _session.SaveChangesAsync();
            return await GetItemAsync(userId);
        }
        public async Task<BaseResult<IReadOnlyList<CartItemResponseDto>>> UpdateAsync(
             Guid userId, Guid bookId, UpdateCartItemRequestDto request)
        {
            if (request.Quantity <= 0)
                return await RemoveAsync(userId, bookId);

            var cart = await _carts.GetActiveByUserAsync(userId);
            if (cart == null)
                return BaseResult<IReadOnlyList<CartItemResponseDto>>.NotFound();

            var item = await _cartItems.GetByCartAndBookAsync(cart.Id, bookId);
            if (item == null)
                return BaseResult<IReadOnlyList<CartItemResponseDto>>.NotFound();

            item.Quantity = request.Quantity;
            _cartItems.Update(item);
            await _session.SaveChangesAsync();

            return await GetItemAsync(userId);
        }
        public async Task<BaseResult<IReadOnlyList<CartItemResponseDto>>> RemoveAsync(Guid userId, Guid bookId)
        {
            var cart = await _carts.GetActiveByUserAsync(userId);
            if (cart == null)
                return BaseResult<IReadOnlyList<CartItemResponseDto>>.NotFound();

            var item = await _cartItems.GetByCartAndBookAsync(cart.Id, bookId);
            if (item == null)
                return BaseResult<IReadOnlyList<CartItemResponseDto>>.NotFound();

            _cartItems.Delete(item);
            await _session.SaveChangesAsync();

            return await GetItemAsync(userId);
        }
    }
}
