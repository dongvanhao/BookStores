using BookStore.Application.Dtos.Ordering_Payment.Cart;
using BookStore.Application.IService.Ordering_Payment;
using BookStore.Application.Mappers.Ordering_Payment;
using BookStore.Domain.Entities.Ordering;
using BookStore.Domain.IRepository.Common;
using BookStore.Shared.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.Services.Ordering_Payment
{
    public class CartItemService : ICartItemService
    {
        private readonly IUnitOfWork _uow;
        public CartItemService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<BaseResult<IReadOnlyList<CartItemResponseDto>>> GetItemAsync(Guid userId)
        {
            var cart = await _uow.Cart.GetActiveByUserAsync(userId);
            if (cart == null)
                return BaseResult<IReadOnlyList<CartItemResponseDto>>.Ok([]);


            var items = await _uow.CartItem.GetByCartIdAsync(cart.Id);
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
                "Số lượng phải lớn hơn 0",
                ErrorType.Validation
            );

            var cart = await _uow.Cart.GetActiveByUserAsync(userId);
            if (cart == null)
                return BaseResult<IReadOnlyList<CartItemResponseDto>>.NotFound("Không tìm thấy giỏ hàng");

            var item = await _uow.CartItem.GetByCartAndBookAsync(cart.Id, request.BookId);

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

                await _uow.CartItem.AddAsync(item);
            }
            else
            {
                item.Quantity += request.Quantity;
                _uow.CartItem.Update(item);
            }

            await _uow.SaveChangesAsync();
            return await GetItemAsync(userId);
        }
        public async Task<BaseResult<IReadOnlyList<CartItemResponseDto>>> UpdateAsync(
             Guid userId, Guid bookId, UpdateCartItemRequestDto request)
        {
            if (request.Quantity <= 0)
                return await RemoveAsync(userId, bookId);

            var cart = await _uow.Cart.GetActiveByUserAsync(userId);
            if (cart == null)
                return BaseResult<IReadOnlyList<CartItemResponseDto>>.NotFound();

            var item = await _uow.CartItem.GetByCartAndBookAsync(cart.Id, bookId);
            if (item == null)
                return BaseResult<IReadOnlyList<CartItemResponseDto>>.NotFound();

            item.Quantity = request.Quantity;
            _uow.CartItem.Update(item);
            await _uow.SaveChangesAsync();

            return await GetItemAsync(userId);
        }
        public async Task<BaseResult<IReadOnlyList<CartItemResponseDto>>> RemoveAsync(Guid userId, Guid bookId)
        {
            var cart = await _uow.Cart.GetActiveByUserAsync(userId);
            if (cart == null)
                return BaseResult<IReadOnlyList<CartItemResponseDto>>.NotFound();

            var item = await _uow.CartItem.GetByCartAndBookAsync(cart.Id, bookId);
            if (item == null)
                return BaseResult<IReadOnlyList<CartItemResponseDto>>.NotFound();

            _uow.CartItem.Delete(item);
            await _uow.SaveChangesAsync();

            return await GetItemAsync(userId);
        }
    }
}
