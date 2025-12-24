using BookStore.Application.Dtos.Ordering_Payment.Cart;
using BookStore.Shared.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.IService.Ordering_Payment
{
    public interface ICartItemService
    {
        Task<BaseResult<IReadOnlyList<CartItemResponseDto>>> GetItemAsync(Guid userId);
        Task<BaseResult<IReadOnlyList<CartItemResponseDto>>> AddAsync(Guid userId, AddCartItemRequestDto request);
        Task<BaseResult<IReadOnlyList<CartItemResponseDto>>> UpdateAsync(Guid userId, Guid bookId, UpdateCartItemRequestDto request);
        Task<BaseResult<IReadOnlyList<CartItemResponseDto>>> RemoveAsync(Guid userId, Guid bookId);
    }
}
