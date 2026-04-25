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
    public class CartService : ICartService
    {
        private readonly ICartRepository _carts;
        private readonly IDbSession _session;
        public CartService(ICartRepository carts, IDbSession session)
        {
            _carts = carts;
            _session = session;
        }

        public async Task<BaseResult<CartResponseDto>> GetCurrentCartAsync(Guid userId)
        {
            var cart = await _carts.GetActiveByUserAsync(userId);
            if (cart == null)
            {
                cart = new Cart
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                };

                await _carts.AddAsync(cart);
                await _session.SaveChangesAsync();
            }
            return BaseResult<CartResponseDto>.Ok(cart.ToResponse());
            
        }
    }
}
