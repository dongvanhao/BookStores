using BookStore.Application.Dtos.Ordering_Payment.Cart;
using BookStore.Domain.Entities.Ordering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.Mappers.Ordering_Payment
{
    public static class CartItemMapping
    {
        public static CartItemResponseDto ToResponse(this CartItem dto)
        {
            return new CartItemResponseDto
            {
                BookId = dto.BookId,
                BookTitle = dto.Book.Title,
                Quantity = dto.Quantity,
                UnitPrice = dto.UnitPrice,
                TotalPrice = dto.TotalPrice
            };
        }
    }
}
