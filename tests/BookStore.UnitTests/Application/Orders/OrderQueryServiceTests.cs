using BookStore.Application.Orders.Queries;
using BookStore.Application.Orders.Services;
using BookStore.Domain.Entities;
using BookStore.Domain.Enums;
using BookStore.Domain.IRepository;
using BookStore.UnitTests.Helpers;
using Moq;

namespace BookStore.UnitTests.Application.Orders;

public class OrderQueryServiceTests
{
    private readonly Mock<IOrderRepository> _mockOrderRepo;
    private readonly OrderQueryService      _sut;

    public OrderQueryServiceTests()
    {
        _mockOrderRepo = new Mock<IOrderRepository>();
        _sut = new OrderQueryService(_mockOrderRepo.Object);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Order CreateOrder(Guid userId, string address = "123 Main St")
        => Order.Create(userId, address, null);

    private static Order CreateOrderWithItems(Guid userId, params (Guid bookId, int qty, decimal price)[] items)
    {
        var order = Order.Create(userId, "123 Main St", null);
        foreach (var (bookId, qty, price) in items)
            order.AddItem(bookId, "Test Book", qty, price);
        return order;
    }

    private void SetupGetQueryable(params Order[] orders)
        => _mockOrderRepo
            .Setup(r => r.GetQueryable())
            .Returns(orders.AsAsyncQueryable());

    // ── GetOrderHistoryAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task GetOrderHistoryAsync_ShouldReturnOnlyCurrentUserOrders()
    {
        // Arrange
        var userId      = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        var myOrder    = CreateOrder(userId);
        var otherOrder = CreateOrder(otherUserId);

        SetupGetQueryable(myOrder, otherOrder);

        var query = new GetOrdersQuery { PageNumber = 1, PageSize = 10 };

        // Act
        var result = await _sut.GetOrderHistoryAsync(query, userId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Items);
        Assert.Equal(myOrder.Id, result.Value.Items[0].Id);
    }

    [Fact]
    public async Task GetOrderHistoryAsync_ShouldFilterByStatus_WhenStatusProvided()
    {
        // Arrange
        var userId = Guid.NewGuid();

        var pendingOrder   = CreateOrder(userId);
        var confirmedOrder = CreateOrder(userId);
        confirmedOrder.Confirm();

        SetupGetQueryable(pendingOrder, confirmedOrder);

        var query = new GetOrdersQuery
        {
            PageNumber = 1,
            PageSize   = 10,
            Status     = OrderStatus.Pending
        };

        // Act
        var result = await _sut.GetOrderHistoryAsync(query, userId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Items);
        Assert.Equal("Pending", result.Value.Items[0].Status);
    }

    // ── GetByIdAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ShouldReturnDetail_WhenOwner()
    {
        // Arrange
        var userId  = Guid.NewGuid();
        var bookId  = Guid.NewGuid();
        var order   = CreateOrderWithItems(userId, (bookId, 2, 150_000m));

        _mockOrderRepo
            .Setup(r => r.GetByIdWithItemsAsync(order.Id, default))
            .ReturnsAsync(order);

        // Act
        var result = await _sut.GetByIdAsync(order.Id, userId, isAdmin: false);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(order.Id, result.Value.Id);
        Assert.Single(result.Value.Items);
        Assert.Equal(bookId, result.Value.Items[0].BookId);
        Assert.Equal(2, result.Value.Items[0].Quantity);
        Assert.Equal(150_000m, result.Value.Items[0].UnitPrice);
        Assert.Equal(300_000m, result.Value.Items[0].SubTotal);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnDetail_WhenAdmin()
    {
        // Arrange
        var ownerUserId   = Guid.NewGuid();
        var adminUserId   = Guid.NewGuid();
        var order         = CreateOrder(ownerUserId);

        _mockOrderRepo
            .Setup(r => r.GetByIdWithItemsAsync(order.Id, default))
            .ReturnsAsync(order);

        // Act
        var result = await _sut.GetByIdAsync(order.Id, adminUserId, isAdmin: true);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(order.Id, result.Value.Id);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNotFound_WhenCustomerAccessesOthersOrder()
    {
        // Arrange
        var ownerUserId    = Guid.NewGuid();
        var requesterUserId = Guid.NewGuid();
        var order           = CreateOrder(ownerUserId);

        _mockOrderRepo
            .Setup(r => r.GetByIdWithItemsAsync(order.Id, default))
            .ReturnsAsync(order);

        // Act
        var result = await _sut.GetByIdAsync(order.Id, requesterUserId, isAdmin: false);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Order.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNotFound_WhenOrderDoesNotExist()
    {
        // Arrange
        var missingId = Guid.NewGuid();

        _mockOrderRepo
            .Setup(r => r.GetByIdWithItemsAsync(missingId, default))
            .ReturnsAsync((Order?)null);

        // Act
        var result = await _sut.GetByIdAsync(missingId, Guid.NewGuid(), isAdmin: false);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Order.NotFound", result.Error.Code);
    }
}
