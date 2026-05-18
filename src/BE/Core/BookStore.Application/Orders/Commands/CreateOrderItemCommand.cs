namespace BookStore.Application.Orders.Commands;

public record CreateOrderItemCommand(Guid BookId, int Quantity);
