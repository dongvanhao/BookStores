using System.ComponentModel.DataAnnotations;

namespace BookStore.Application.Dtos.Ordering_Payment.Cart
{
    public record AddCartItemRequestDto(
        [Required] Guid BookId,
        [Range(1, 100)] int Quantity
    );
}
