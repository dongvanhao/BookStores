using System.ComponentModel.DataAnnotations;

namespace BookStore.Application.Dtos.Ordering_Payment.Cart
{
    public record UpdateCartItemRequestDto(
        [Range(1, 100)] int Quantity
    );
}
