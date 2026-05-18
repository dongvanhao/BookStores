namespace BookStore.Application.Orders.DTOs;

public record OrderDto(
    Guid Id,
    string Status,
    decimal TotalAmount,
    int ItemCount,
    string ShippingAddress,
    DateTime CreatedAt
);
