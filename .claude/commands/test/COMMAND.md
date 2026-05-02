---
name: test
description: TDD cho BookStore — xUnit + Moq, RED-GREEN-REFACTOR, Prove-It cho bug fix
---

# /test — Test-Driven Development

> "Tests are proof, not afterthought."

## For New Features: RED-GREEN-REFACTOR

### RED — Viết failing test trước

```csharp
// BookStore.UnitTests/Application/Books/BookServiceTests.cs
[Fact]
public async Task GetByIdAsync_ShouldReturnBookDto_WhenBookExists()
{
    // Arrange
    var bookId = Guid.NewGuid();
    var book = Book.Create("Clean Code", 150_000m, Guid.NewGuid());
    _mockRepo.Setup(r => r.GetByIdAsync(bookId, default)).ReturnsAsync(book);

    // Act
    var result = await _sut.GetByIdAsync(bookId);

    // Assert
    Assert.True(result.IsSuccess);
    Assert.Equal(book.Title, result.Value.Title);
}

[Fact]
public async Task GetByIdAsync_ShouldReturnNotFound_WhenBookMissing()
{
    // Arrange
    var bookId = Guid.NewGuid();
    _mockRepo.Setup(r => r.GetByIdAsync(bookId, default)).ReturnsAsync((Book?)null);

    // Act
    var result = await _sut.GetByIdAsync(bookId);

    // Assert
    Assert.False(result.IsSuccess);
    Assert.Equal(ErrorType.NotFound, result.Error.Type);
    Assert.Equal("Book.NotFound", result.Error.Code);
}
```

Chạy test → **FAIL** (chứng minh chưa có implementation).

### GREEN — Minimal implementation

```csharp
public async Task<Result<BookDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
{
    var book = await _repo.GetByIdAsync(id, ct);
    if (book is null) return BookErrors.NotFound(id);
    return book.ToDto();
}
```

Chạy test → **PASS**.

### REFACTOR — Dọn dẹp, test vẫn xanh

---

## For Bug Fixes: Prove-It Pattern

### Step 1: Viết test reproduce bug (phải FAIL)

```csharp
[Fact]
public void Cancel_ShouldFail_WhenOrderIsDelivered()
{
    // Arrange — tạo order ở trạng thái Delivered
    var order = CreateOrderWithStatus(OrderStatus.Delivered);

    // Act
    var result = order.Cancel();

    // Assert — test này FAIL nếu bug chưa được fix
    Assert.False(result.IsSuccess);
    Assert.Equal("Order.CannotCancel", result.Error.Code);
}
```

### Step 2: Verify FAIL → chứng minh bug tồn tại
### Step 3: Fix bug
### Step 4: Verify PASS → chứng minh fix đúng
### Step 5: Chạy full suite — không regression

```bash
dotnet test tests/BookStore.UnitTests
```

---

## Test Pyramid (BookStore)

| Level | Tỷ lệ | Scope |
|-------|-------|-------|
| **Unit** | ~90% | Service + Domain logic (mock repository) |
| **Integration** | ~10% | Chưa ưu tiên — API + DB thực |
| **E2E** | Không | Ngoài scope dự án hiện tại |

---

## Naming Convention

```csharp
// Pattern: {Method}_Should{Expected}_When{Condition}
GetByIdAsync_ShouldReturnBookDto_WhenBookExists
GetByIdAsync_ShouldReturnNotFound_WhenBookMissing
CreateAsync_ShouldFail_WhenTitleAlreadyExists
Cancel_ShouldSucceed_WhenOrderIsPending
Cancel_ShouldFail_WhenOrderIsDelivered
```

---

## Domain State Machine — dùng [Theory]

```csharp
[Theory]
[InlineData(OrderStatus.Shipped)]
[InlineData(OrderStatus.Delivered)]
public void Cancel_ShouldFail_WhenOrderCannotBeCancelled(OrderStatus status)
{
    var order = CreateOrderWithStatus(status);
    var result = order.Cancel();
    Assert.False(result.IsSuccess);
    Assert.Equal("Order.CannotCancel", result.Error.Code);
}

[Theory]
[InlineData(OrderStatus.Pending)]
[InlineData(OrderStatus.Confirmed)]
public void Cancel_ShouldSucceed_WhenOrderIsCancellable(OrderStatus status)
{
    var order = CreateOrderWithStatus(status);
    var result = order.Cancel();
    Assert.True(result.IsSuccess);
    Assert.Equal(OrderStatus.Cancelled, order.Status);
}
```

---

## Result Pattern Testing

```csharp
// Luôn test cả hai path: Success và Failure
[Fact]
public async Task CreateAsync_ShouldReturnBookId_WhenTitleIsUnique()
{
    _mockRepo.Setup(r => r.ExistsByTitleAsync(It.IsAny<string>(), default)).ReturnsAsync(false);
    var result = await _sut.CreateAsync(new CreateBookRequest { Title = "New", Price = 100 });
    Assert.True(result.IsSuccess);
}

[Fact]
public async Task CreateAsync_ShouldReturnConflict_WhenTitleExists()
{
    _mockRepo.Setup(r => r.ExistsByTitleAsync(It.IsAny<string>(), default)).ReturnsAsync(true);
    var result = await _sut.CreateAsync(new CreateBookRequest { Title = "Dup", Price = 100 });
    Assert.False(result.IsSuccess);
    Assert.Equal(ErrorType.Conflict, result.Error.Type);
}
```

---

## Anti-Patterns

| Anti-Pattern | Vấn đề | Fix |
|--------------|--------|-----|
| Test implementation detail | Break khi refactor | Test input/output (Result) |
| Shared state giữa các test | Test ảnh hưởng nhau | `new Mock<>()` trong constructor |
| Over-mocking | False confidence | Mock chỉ ở boundary (repository) |
| Assert không check Error.Code | Bỏ sót sai lỗi | Luôn check cả `ErrorType` và `Code` |
| Không test Failure path | Bỏ sót bug | Mỗi Service method cần ≥ 1 Failure test |

---

## Verification Checklist

```markdown
- [ ] Mỗi Service method có test Success + Failure path
- [ ] Domain state machine có [Theory] test cho mọi transition
- [ ] Bug fix có reproduction test viết trước khi fix
- [ ] Test name đúng pattern: Method_ShouldX_WhenY
- [ ] Mock chỉ ở IRepository, không mock Service khác
- [ ] dotnet test pass toàn bộ
```
