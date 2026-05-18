using BookStore.Application.Orders.Commands;
using BookStore.Application.Orders.Services;
using BookStore.Domain.Entities;
using BookStore.Domain.IRepository;
using BookStore.UnitTests.Helpers;
using Moq;

namespace BookStore.UnitTests.Application.Orders;

public class OrderCommandServiceTests
{
    private readonly Mock<IOrderRepository> _mockOrderRepo;
    private readonly Mock<IBookRepository>  _mockBookRepo;
    private readonly Mock<IUnitOfWork>      _mockUow;
    private readonly OrderCommandService    _sut;

    public OrderCommandServiceTests()
    {
        _mockOrderRepo = new Mock<IOrderRepository>();
        _mockBookRepo  = new Mock<IBookRepository>();
        _mockUow       = new Mock<IUnitOfWork>();
        _sut = new OrderCommandService(
            _mockOrderRepo.Object,
            _mockBookRepo.Object,
            _mockUow.Object);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    // Creates a Book via factory then patches its Id using reflection (protected set)
    private static Book CreateBook(Guid id, int stock = 10, decimal price = 150_000m)
    {
        var book = Book.Create("Test Book", null, "ISBN-001", 2023, price, stock, Guid.NewGuid());
        typeof(BookStore.Domain.Common.BaseEntity)
            .GetProperty(nameof(BookStore.Domain.Common.BaseEntity.Id))!
            .SetValue(book, id);
        return book;
    }

    private void SetupBooksQueryable(params Book[] books)
    {
        _mockBookRepo
            .Setup(r => r.GetQueryable())
            .Returns(books.AsAsyncQueryable());
    }

    private static CreateOrderCommand BuildCommand(string address, params (Guid bookId, int qty)[] items)
    {
        var itemCmds = items
            .Select(i => new CreateOrderItemCommand(i.bookId, i.qty))
            .ToList();
        return new CreateOrderCommand(address, null, itemCmds);
    }

    // ── CreateAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_ShouldReturnOrderId_WhenAllBooksExistAndHaveStock()
    {
        // Arrange
        var bookId = Guid.NewGuid();
        var book   = CreateBook(bookId, stock: 5);
        SetupBooksQueryable(book);

        var cmd = BuildCommand("123 Main St", (bookId, 2));

        // Act
        var result = await _sut.CreateAsync(cmd, Guid.NewGuid());

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value);
    }

    [Fact]
    public async Task CreateAsync_ShouldFail_WhenItemsListIsEmpty()
    {
        // Arrange
        var cmd = new CreateOrderCommand("123 Main St", null, []);

        // Act
        var result = await _sut.CreateAsync(cmd, Guid.NewGuid());

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Order.Empty", result.Error.Code);
    }

    [Fact]
    public async Task CreateAsync_ShouldFail_WhenBookNotFound()
    {
        // Arrange
        SetupBooksQueryable(); // no books in DB
        var cmd = BuildCommand("123 Main St", (Guid.NewGuid(), 1));

        // Act
        var result = await _sut.CreateAsync(cmd, Guid.NewGuid());

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Book.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task CreateAsync_ShouldFail_WhenInsufficientStock()
    {
        // Arrange
        var bookId = Guid.NewGuid();
        var book   = CreateBook(bookId, stock: 1); // only 1 in stock
        SetupBooksQueryable(book);

        var cmd = BuildCommand("123 Main St", (bookId, 5)); // requesting 5

        // Act
        var result = await _sut.CreateAsync(cmd, Guid.NewGuid());

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Book.InsufficientStock", result.Error.Code);
    }

    [Fact]
    public async Task CreateAsync_ShouldReduceStockForAllItems_WhenSuccessful()
    {
        // Arrange
        var bookId1 = Guid.NewGuid();
        var bookId2 = Guid.NewGuid();
        var book1   = CreateBook(bookId1, stock: 10);
        var book2   = CreateBook(bookId2, stock: 10);
        SetupBooksQueryable(book1, book2);

        var cmd = BuildCommand("123 Main St", (bookId1, 3), (bookId2, 4));

        // Act
        var result = await _sut.CreateAsync(cmd, Guid.NewGuid());

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(7, book1.StockQuantity); // 10 - 3
        Assert.Equal(6, book2.StockQuantity); // 10 - 4
    }

    [Fact]
    public async Task CreateAsync_ShouldCallSaveChanges_WhenSuccessful()
    {
        // Arrange
        var bookId = Guid.NewGuid();
        var book   = CreateBook(bookId, stock: 10);
        SetupBooksQueryable(book);

        var cmd = BuildCommand("123 Main St", (bookId, 1));

        // Act
        await _sut.CreateAsync(cmd, Guid.NewGuid());

        // Assert
        _mockUow.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    // ── ConfirmAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task ConfirmAsync_ShouldSucceed_WhenOrderIsPending()
    {
        // Arrange
        var order = Order.Create(Guid.NewGuid(), "123 Main St", null);
        _mockOrderRepo.Setup(r => r.GetByIdAsync(order.Id, default)).ReturnsAsync(order);

        // Act
        var result = await _sut.ConfirmAsync(order.Id);

        // Assert
        Assert.True(result.IsSuccess);
        _mockUow.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task ConfirmAsync_ShouldFail_WhenOrderNotFound()
    {
        // Arrange
        var missingId = Guid.NewGuid();
        _mockOrderRepo.Setup(r => r.GetByIdAsync(missingId, default)).ReturnsAsync((Order?)null);

        // Act
        var result = await _sut.ConfirmAsync(missingId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Order.NotFound", result.Error.Code);
        _mockUow.Verify(u => u.SaveChangesAsync(default), Times.Never);
    }

    [Fact]
    public async Task ConfirmAsync_ShouldFail_WhenOrderNotPending()
    {
        // Arrange
        var order = Order.Create(Guid.NewGuid(), "123 Main St", null);
        order.Confirm(); // already Confirmed
        _mockOrderRepo.Setup(r => r.GetByIdAsync(order.Id, default)).ReturnsAsync(order);

        // Act
        var result = await _sut.ConfirmAsync(order.Id);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Order.InvalidTransition", result.Error.Code);
        _mockUow.Verify(u => u.SaveChangesAsync(default), Times.Never);
    }

    // ── ShipAsync ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task ShipAsync_ShouldSucceed_WhenOrderIsConfirmed()
    {
        // Arrange
        var order = Order.Create(Guid.NewGuid(), "123 Main St", null);
        order.Confirm();
        _mockOrderRepo.Setup(r => r.GetByIdAsync(order.Id, default)).ReturnsAsync(order);

        // Act
        var result = await _sut.ShipAsync(order.Id);

        // Assert
        Assert.True(result.IsSuccess);
        _mockUow.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task ShipAsync_ShouldFail_WhenOrderNotFound()
    {
        // Arrange
        var missingId = Guid.NewGuid();
        _mockOrderRepo.Setup(r => r.GetByIdAsync(missingId, default)).ReturnsAsync((Order?)null);

        // Act
        var result = await _sut.ShipAsync(missingId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Order.NotFound", result.Error.Code);
        _mockUow.Verify(u => u.SaveChangesAsync(default), Times.Never);
    }

    [Fact]
    public async Task ShipAsync_ShouldFail_WhenOrderNotConfirmed()
    {
        // Arrange
        var order = Order.Create(Guid.NewGuid(), "123 Main St", null); // Pending, not Confirmed
        _mockOrderRepo.Setup(r => r.GetByIdAsync(order.Id, default)).ReturnsAsync(order);

        // Act
        var result = await _sut.ShipAsync(order.Id);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Order.InvalidTransition", result.Error.Code);
        _mockUow.Verify(u => u.SaveChangesAsync(default), Times.Never);
    }

    // ── DeliverAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task DeliverAsync_ShouldSucceed_WhenOrderIsShipped()
    {
        // Arrange
        var order = Order.Create(Guid.NewGuid(), "123 Main St", null);
        order.Confirm();
        order.Ship();
        _mockOrderRepo.Setup(r => r.GetByIdAsync(order.Id, default)).ReturnsAsync(order);

        // Act
        var result = await _sut.DeliverAsync(order.Id);

        // Assert
        Assert.True(result.IsSuccess);
        _mockUow.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task DeliverAsync_ShouldFail_WhenOrderNotFound()
    {
        // Arrange
        var missingId = Guid.NewGuid();
        _mockOrderRepo.Setup(r => r.GetByIdAsync(missingId, default)).ReturnsAsync((Order?)null);

        // Act
        var result = await _sut.DeliverAsync(missingId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Order.NotFound", result.Error.Code);
        _mockUow.Verify(u => u.SaveChangesAsync(default), Times.Never);
    }

    [Fact]
    public async Task DeliverAsync_ShouldFail_WhenOrderNotShipped()
    {
        // Arrange
        var order = Order.Create(Guid.NewGuid(), "123 Main St", null);
        order.Confirm(); // Confirmed, not Shipped
        _mockOrderRepo.Setup(r => r.GetByIdAsync(order.Id, default)).ReturnsAsync(order);

        // Act
        var result = await _sut.DeliverAsync(order.Id);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Order.InvalidTransition", result.Error.Code);
        _mockUow.Verify(u => u.SaveChangesAsync(default), Times.Never);
    }

    // ── CancelAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task CancelAsync_ShouldSucceed_WhenOrderIsPending()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var order  = Order.Create(userId, "123 Main St", null);
        _mockOrderRepo.Setup(r => r.GetByIdWithItemsAsync(order.Id, default)).ReturnsAsync(order);
        _mockBookRepo.Setup(r => r.GetQueryable()).Returns(Array.Empty<Book>().AsAsyncQueryable());

        // Act
        var result = await _sut.CancelAsync(order.Id, userId, isAdmin: false);

        // Assert
        Assert.True(result.IsSuccess);
        _mockUow.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task CancelAsync_ShouldSucceed_WhenOrderIsConfirmed()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var order  = Order.Create(userId, "123 Main St", null);
        order.Confirm();
        _mockOrderRepo.Setup(r => r.GetByIdWithItemsAsync(order.Id, default)).ReturnsAsync(order);
        _mockBookRepo.Setup(r => r.GetQueryable()).Returns(Array.Empty<Book>().AsAsyncQueryable());

        // Act
        var result = await _sut.CancelAsync(order.Id, userId, isAdmin: false);

        // Assert
        Assert.True(result.IsSuccess);
        _mockUow.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task CancelAsync_ShouldFail_WhenOrderNotFound()
    {
        // Arrange
        var missingId = Guid.NewGuid();
        _mockOrderRepo.Setup(r => r.GetByIdWithItemsAsync(missingId, default)).ReturnsAsync((Order?)null);

        // Act
        var result = await _sut.CancelAsync(missingId, Guid.NewGuid(), isAdmin: false);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Order.NotFound", result.Error.Code);
        _mockUow.Verify(u => u.SaveChangesAsync(default), Times.Never);
    }

    [Fact]
    public async Task CancelAsync_ShouldFail_WhenOrderIsShipped()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var order  = Order.Create(userId, "123 Main St", null);
        order.Confirm();
        order.Ship();
        _mockOrderRepo.Setup(r => r.GetByIdWithItemsAsync(order.Id, default)).ReturnsAsync(order);

        // Act
        var result = await _sut.CancelAsync(order.Id, userId, isAdmin: false);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Order.CannotCancel", result.Error.Code);
        _mockUow.Verify(u => u.SaveChangesAsync(default), Times.Never);
    }

    [Fact]
    public async Task CancelAsync_ShouldFail_WhenCustomerCancelsOthersOrder()
    {
        // Arrange
        var ownerUserId     = Guid.NewGuid();
        var requesterUserId = Guid.NewGuid();
        var order           = Order.Create(ownerUserId, "123 Main St", null);
        _mockOrderRepo.Setup(r => r.GetByIdWithItemsAsync(order.Id, default)).ReturnsAsync(order);

        // Act
        var result = await _sut.CancelAsync(order.Id, requesterUserId, isAdmin: false);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Order.NotFound", result.Error.Code);
        _mockUow.Verify(u => u.SaveChangesAsync(default), Times.Never);
    }

    [Fact]
    public async Task CancelAsync_ShouldRestoreStockForAllItems_WhenSuccessful()
    {
        // Arrange
        var userId  = Guid.NewGuid();
        var bookId  = Guid.NewGuid();
        var book    = CreateBook(bookId, stock: 5);

        var order = Order.Create(userId, "123 Main St", null);
        order.AddItem(bookId, "Test Book", 3, 150_000m); // 3 units ordered

        _mockOrderRepo.Setup(r => r.GetByIdWithItemsAsync(order.Id, default)).ReturnsAsync(order);
        SetupBooksQueryable(book);

        // Act
        var result = await _sut.CancelAsync(order.Id, userId, isAdmin: false);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(8, book.StockQuantity); // 5 + 3 restored
    }

    [Fact]
    public async Task CancelAsync_ShouldNotCallSaveChanges_WhenCancelFails()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var order  = Order.Create(userId, "123 Main St", null);
        order.Confirm();
        order.Ship(); // Shipped — cannot cancel

        _mockOrderRepo.Setup(r => r.GetByIdWithItemsAsync(order.Id, default)).ReturnsAsync(order);

        // Act
        var result = await _sut.CancelAsync(order.Id, userId, isAdmin: false);

        // Assert
        Assert.False(result.IsSuccess);
        _mockUow.Verify(u => u.SaveChangesAsync(default), Times.Never);
    }
}
