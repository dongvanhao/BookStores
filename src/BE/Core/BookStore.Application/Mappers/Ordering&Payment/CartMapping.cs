using BookStore.Application.Dtos.Ordering_Payment.Cart;
using BookStore.Domain.Entities.Ordering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.Mappers.Ordering_Payment
{
    public static class CartMapping
    {
        public static CartResponseDto ToResponse(this Cart cart)
        {
            return new CartResponseDto
            {
                Id = cart.Id,
                IsActive = cart.IsActive,
                CreatedAt = cart.CreatedAt,
                TotalItems = cart.Items.Count
            };
        }
    }
}
