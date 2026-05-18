namespace BookStore.Application.Orders.Commands;

public record CreateOrderCommand(
    string ShippingAddress,
    string? Note,
    IReadOnlyList<CreateOrderItemCommand> Items
);
