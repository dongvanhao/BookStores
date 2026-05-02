using BookStore.Domain.Common;
using BookStore.Domain.Enums;
using BookStore.Domain.Errors;
using BookStore.Shared.Results;

namespace BookStore.Domain.Entities;

public class Order : BaseEntity
{
    public OrderStatus Status { get; private set; }
    public decimal TotalAmount { get; private set; }
    public string ShippingAddress { get; private set; } = string.Empty;
    public string? Note { get; private set; }

    // FK
    public Guid UserId { get; private set; }
    public ApplicationUser User { get; private set; } = null!;

    // Navigation
    public ICollection<OrderItem> Items { get; private set; } = [];

    private Order() { }

    public static Order Create(Guid userId, string shippingAddress, string? note)
    {
        return new Order
        {
            Id              = Guid.NewGuid(),
            UserId          = userId,
            Status          = OrderStatus.Pending,
            TotalAmount     = 0,
            ShippingAddress = shippingAddress,
            Note            = note,
            Items           = [],
            CreatedAt       = DateTime.UtcNow,
            UpdatedAt       = DateTime.UtcNow
        };
    }

    public Result AddItem(Guid bookId, string bookTitle, int quantity, decimal unitPrice)
    {
        if (Status != OrderStatus.Pending)
            return Result.Failure(OrderErrors.InvalidTransition(Status, OrderStatus.Pending));

        var item = OrderItem.Create(Id, bookId, bookTitle, quantity, unitPrice);
        Items.Add(item);
        RecalculateTotal();
        return Result.Success();
    }

    public void RecalculateTotal()
    {
        TotalAmount = Items.Sum(i => i.SubTotal);
        UpdatedAt   = DateTime.UtcNow;
    }

    // ─── State Machine ────────────────────────────────────────────────────────

    /// <summary>
    /// Pending → Confirmed
    /// </summary>
    public Result Confirm()
    {
        if (Status != OrderStatus.Pending)
            return Result.Failure(OrderErrors.InvalidTransition(Status, OrderStatus.Confirmed));

        Status    = OrderStatus.Confirmed;
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    /// <summary>
    /// Confirmed → Shipped
    /// </summary>
    public Result Ship()
    {
        if (Status != OrderStatus.Confirmed)
            return Result.Failure(OrderErrors.InvalidTransition(Status, OrderStatus.Shipped));

        Status    = OrderStatus.Shipped;
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    /// <summary>
    /// Shipped → Delivered
    /// </summary>
    public Result Deliver()
    {
        if (Status != OrderStatus.Shipped)
            return Result.Failure(OrderErrors.InvalidTransition(Status, OrderStatus.Delivered));

        Status    = OrderStatus.Delivered;
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    /// <summary>
    /// Pending | Confirmed → Cancelled
    /// </summary>
    public Result Cancel()
    {
        if (Status is OrderStatus.Shipped or OrderStatus.Delivered or OrderStatus.Cancelled)
            return Result.Failure(OrderErrors.CannotCancel(Status));

        Status    = OrderStatus.Cancelled;
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }
}

