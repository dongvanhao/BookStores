# Testing Standards — BookStore (xUnit + Moq)

## Scope & Pyramid

```
      [Integration]   ← Ít, test API endpoint thực (không ưu tiên hiện tại)
    [Unit Tests]       ← Chủ yếu: Application Service + Domain logic
```

**Ưu tiên test:**
1. Order state machine (valid/invalid transitions)
2. Result Pattern paths (Success / Failure)
3. Business rule validation trong Service (mock repository)
4. Auth logic (password hash, token generation)

**Công cụ:** xUnit / NUnit + Moq

## File Organization

```
tests/
  BookStore.UnitTests/
    Application/
      Books/
        BookServiceTests.cs
      Orders/
        OrderServiceTests.cs
    Domain/
      Orders/
        OrderTests.cs          ← State machine
    Common/
      ResultTests.cs
    Mocks/
      MockBookRepository.cs
```

## Unit Test — Service (mock repository)

```csharp
// Application/Books/BookServiceTests.cs
public class BookServiceTests
{
    private readonly Mock<IBookRepository> _mockRepo;
    private readonly BookService _sut;

    public BookServiceTests()
    {
        _mockRepo = new Mock<IBookRepository>();
        _sut = new BookService(_mockRepo.Object);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnBook_WhenFound()
    {
        // Arrange
        var bookId = Guid.NewGuid();
        var book = Book.Create("Clean Code", 150_000m);
        _mockRepo.Setup(r => r.GetByIdAsync(bookId, default)).ReturnsAsync(book);

        // Act
        var result = await _sut.GetByIdAsync(bookId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(book.Title, result.Value.Title);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNotFound_WhenMissing()
    {
        // Arrange
        var bookId = Guid.NewGuid();
        _mockRepo.Setup(r => r.GetByIdAsync(bookId, default)).ReturnsAsync((Book?)null);

        // Act
        var result = await _sut.GetByIdAsync(bookId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task CreateAsync_ShouldFail_WhenTitleAlreadyExists()
    {
        // Arrange
        var command = new CreateBookCommand { Title = "Existing Book", Price = 100_000m };
        _mockRepo.Setup(r => r.ExistsByTitleAsync(command.Title, default)).ReturnsAsync(true);

        // Act
        var result = await _sut.CreateAsync(command);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Book.TitleExists", result.Error.Code);
    }
}
```

## Unit Test — Domain (state machine)

```csharp
// Domain/Orders/OrderTests.cs
public class OrderTests
{
    [Fact]
    public void Cancel_ShouldSucceed_WhenOrderIsPending()
    {
        var order = Order.Create(userId: Guid.NewGuid(), items: SampleItems());

        var result = order.Cancel();

        Assert.True(result.IsSuccess);
        Assert.Equal(OrderStatus.Cancelled, order.Status);
    }

    [Theory]
    [InlineData(OrderStatus.Shipped)]
    [InlineData(OrderStatus.Delivered)]
    public void Cancel_ShouldFail_WhenOrderIsShippedOrDelivered(OrderStatus status)
    {
        var order = CreateOrderWithStatus(status);

        var result = order.Cancel();

        Assert.False(result.IsSuccess);
        Assert.Equal("Order.CannotCancel", result.Error.Code);
    }

    [Fact]
    public void Confirm_ShouldSucceed_WhenOrderIsPending()
    {
        var order = Order.Create(userId: Guid.NewGuid(), items: SampleItems());

        var result = order.Confirm();

        Assert.True(result.IsSuccess);
        Assert.Equal(OrderStatus.Confirmed, order.Status);
    }
}
```

## Unit Test — Result Pattern

```csharp
// Common/ResultTests.cs
public class ResultTests
{
    [Fact]
    public void Success_ShouldHaveIsSuccessTrue()
    {
        var result = Result.Success();
        Assert.True(result.IsSuccess);
        Assert.Equal(Error.None, result.Error);
    }

    [Fact]
    public void Failure_ShouldHaveIsSuccessFalse()
    {
        var error = Error.NotFound("Book.NotFound", "Not found.");
        var result = Result.Failure(error);

        Assert.False(result.IsSuccess);
        Assert.Equal(error, result.Error);
    }

    [Fact]
    public void ResultT_ImplicitConversion_FromValue_ShouldSucceed()
    {
        Result<string> result = "hello";
        Assert.True(result.IsSuccess);
        Assert.Equal("hello", result.Value);
    }
}
```

## Naming Conventions

| Thành phần | Pattern | Ví dụ |
|------------|---------|-------|
| Test class | `{Class}Tests` | `BookServiceTests` |
| Test method | `{Method}_Should{Expected}_When{Condition}` | `GetByIdAsync_ShouldReturnNotFound_WhenMissing` |
| SUT variable | `_sut` (System Under Test) | `_sut = new BookService(...)` |
| AAA sections | `// Arrange / // Act / // Assert` | Bắt buộc comment 3 phần |

## Test Commands

```bash
# Chạy tất cả unit test
dotnet test tests/BookStore.UnitTests

# Chạy với coverage
dotnet test tests/BookStore.UnitTests --collect:"XPlat Code Coverage"

# Chạy theo filter (test cụ thể)
dotnet test --filter "FullyQualifiedName~OrderTests"
dotnet test --filter "FullyQualifiedName~BookServiceTests"

# Chạy watch mode
dotnet watch test --project tests/BookStore.UnitTests
```

## Rules
1. Mỗi bug fix → thêm regression test trước khi fix
2. Không test implementation detail — test behavior
3. Mock chỉ ở boundary (repository, external service) — không mock Service khác
4. `[Theory] + [InlineData]` cho các case nhiều input (state machine)
5. Test phải độc lập, không chia sẻ state giữa các test
