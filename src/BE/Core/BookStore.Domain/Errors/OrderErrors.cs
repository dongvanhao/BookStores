using BookStore.Domain.Enums;
using BookStore.Shared.Results;

namespace BookStore.Domain.Errors;

public static class OrderErrors
{
    public static Error InvalidTransition(OrderStatus from, OrderStatus to)
        => Error.Validation(
            "Order.InvalidTransition",
            $"Cannot transition order from '{from}' to '{to}'.");

    public static Error CannotCancel(OrderStatus currentStatus)
        => Error.Validation(
            "Order.CannotCancel",
            $"Cannot cancel an order with status '{currentStatus}'.");

    public static Error NotFound(Guid id)
        => Error.NotFound("Order.NotFound", $"Order '{id}' was not found.");

    public static readonly Error Empty
        = Error.Validation("Order.Empty", "Order must contain at least one item.");
}
