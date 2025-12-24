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
    public class CartService : ICartService
    {
        private readonly IUnitOfWork _uow;
        public CartService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<BaseResult<CartResponseDto>> GetCurrentCartAsync(Guid userId)
        {
            var cart = await _uow.Cart.GetActiveByUserAsync(userId);
            if (cart == null)
            {
                cart = new Cart
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                };

                await _uow.Cart.AddAsync(cart);
                await _uow.SaveChangesAsync();
            }
            return BaseResult<CartResponseDto>.Ok(cart.ToResponse());
            
        }
    }
}
