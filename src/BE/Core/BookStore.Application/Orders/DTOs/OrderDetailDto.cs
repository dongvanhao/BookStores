namespace BookStore.Application.Orders.DTOs;

public record OrderDetailDto(
    Guid Id,
    string Status,
    decimal TotalAmount,
    string ShippingAddress,
    string? Note,
    DateTime CreatedAt,
    IReadOnlyList<OrderItemDto> Items
);
