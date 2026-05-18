using BookStore.Domain.Entities;
using BookStore.Domain.Enums;

namespace BookStore.UnitTests.Domain.Orders;

public class OrderTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Order CreatePendingOrder()
        => Order.Create(Guid.NewGuid(), "123 Test Street", null);

    private static Order CreateConfirmedOrder()
    {
        var order = CreatePendingOrder();
        order.Confirm();
        return order;
    }

    private static Order CreateShippedOrder()
    {
        var order = CreateConfirmedOrder();
        order.Ship();
        return order;
    }

    private static Order CreateDeliveredOrder()
    {
        var order = CreateShippedOrder();
        order.Deliver();
        return order;
    }

    private static Order CreateCancelledOrder()
    {
        var order = CreatePendingOrder();
        order.Cancel();
        return order;
    }

    // ── Confirm ───────────────────────────────────────────────────────────────

    [Fact]
    public void Confirm_ShouldSucceed_WhenPending()
    {
        // Arrange
        var order = CreatePendingOrder();

        // Act
        var result = order.Confirm();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(OrderStatus.Confirmed, order.Status);
    }

    [Theory]
    [InlineData(OrderStatus.Confirmed)]
    [InlineData(OrderStatus.Shipped)]
    [InlineData(OrderStatus.Delivered)]
    [InlineData(OrderStatus.Cancelled)]
    public void Confirm_ShouldFail_WhenNotPending(OrderStatus status)
    {
        // Arrange
        var order = status switch
        {
            OrderStatus.Confirmed  => CreateConfirmedOrder(),
            OrderStatus.Shipped    => CreateShippedOrder(),
            OrderStatus.Delivered  => CreateDeliveredOrder(),
            OrderStatus.Cancelled  => CreateCancelledOrder(),
            _                      => throw new ArgumentOutOfRangeException(nameof(status))
        };

        // Act
        var result = order.Confirm();

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Order.InvalidTransition", result.Error.Code);
    }

    // ── Ship ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Ship_ShouldSucceed_WhenConfirmed()
    {
        // Arrange
        var order = CreateConfirmedOrder();

        // Act
        var result = order.Ship();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(OrderStatus.Shipped, order.Status);
    }

    [Theory]
    [InlineData(OrderStatus.Pending)]
    [InlineData(OrderStatus.Shipped)]
    [InlineData(OrderStatus.Delivered)]
    [InlineData(OrderStatus.Cancelled)]
    public void Ship_ShouldFail_WhenNotConfirmed(OrderStatus status)
    {
        // Arrange
        var order = status switch
        {
            OrderStatus.Pending    => CreatePendingOrder(),
            OrderStatus.Shipped    => CreateShippedOrder(),
            OrderStatus.Delivered  => CreateDeliveredOrder(),
            OrderStatus.Cancelled  => CreateCancelledOrder(),
            _                      => throw new ArgumentOutOfRangeException(nameof(status))
        };

        // Act
        var result = order.Ship();

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Order.InvalidTransition", result.Error.Code);
    }

    // ── Deliver ───────────────────────────────────────────────────────────────

    [Fact]
    public void Deliver_ShouldSucceed_WhenShipped()
    {
        // Arrange
        var order = CreateShippedOrder();

        // Act
        var result = order.Deliver();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(OrderStatus.Delivered, order.Status);
    }

    [Theory]
    [InlineData(OrderStatus.Pending)]
    [InlineData(OrderStatus.Confirmed)]
    [InlineData(OrderStatus.Delivered)]
    [InlineData(OrderStatus.Cancelled)]
    public void Deliver_ShouldFail_WhenNotShipped(OrderStatus status)
    {
        // Arrange
        var order = status switch
        {
            OrderStatus.Pending    => CreatePendingOrder(),
            OrderStatus.Confirmed  => CreateConfirmedOrder(),
            OrderStatus.Delivered  => CreateDeliveredOrder(),
            OrderStatus.Cancelled  => CreateCancelledOrder(),
            _                      => throw new ArgumentOutOfRangeException(nameof(status))
        };

        // Act
        var result = order.Deliver();

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Order.InvalidTransition", result.Error.Code);
    }

    // ── Cancel ────────────────────────────────────────────────────────────────

    [Fact]
    public void Cancel_ShouldSucceed_WhenPending()
    {
        // Arrange
        var order = CreatePendingOrder();

        // Act
        var result = order.Cancel();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(OrderStatus.Cancelled, order.Status);
    }

    [Fact]
    public void Cancel_ShouldSucceed_WhenConfirmed()
    {
        // Arrange
        var order = CreateConfirmedOrder();

        // Act
        var result = order.Cancel();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(OrderStatus.Cancelled, order.Status);
    }

    [Theory]
    [InlineData(OrderStatus.Shipped)]
    [InlineData(OrderStatus.Delivered)]
    [InlineData(OrderStatus.Cancelled)]
    public void Cancel_ShouldFail_WhenShippedOrDeliveredOrCancelled(OrderStatus status)
    {
        // Arrange
        var order = status switch
        {
            OrderStatus.Shipped    => CreateShippedOrder(),
            OrderStatus.Delivered  => CreateDeliveredOrder(),
            OrderStatus.Cancelled  => CreateCancelledOrder(),
            _                      => throw new ArgumentOutOfRangeException(nameof(status))
        };

        // Act
        var result = order.Cancel();

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Order.CannotCancel", result.Error.Code);
    }

    // ── AddItem ───────────────────────────────────────────────────────────────

    [Fact]
    public void AddItem_ShouldFail_WhenNotPending()
    {
        // Arrange
        var order = CreateConfirmedOrder();

        // Act
        var result = order.AddItem(Guid.NewGuid(), "Clean Code", 1, 150_000m);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Order.InvalidTransition", result.Error.Code);
    }

    [Fact]
    public void AddItem_ShouldRecalculateTotal_WhenAdded()
    {
        // Arrange
        var order = CreatePendingOrder();

        // Act
        var result1 = order.AddItem(Guid.NewGuid(), "Clean Code", 2, 150_000m);
        var result2 = order.AddItem(Guid.NewGuid(), "DDD", 1, 200_000m);

        // Assert
        Assert.True(result1.IsSuccess);
        Assert.True(result2.IsSuccess);
        Assert.Equal(500_000m, order.TotalAmount); // 2*150_000 + 1*200_000
    }
}
