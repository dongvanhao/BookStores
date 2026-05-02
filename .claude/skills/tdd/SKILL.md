---
name: Test-Driven Development
description: RED-GREEN-REFACTOR cho BookStore — xUnit + Moq, Result Pattern, state machine
---

# TDD Skill — BookStore (.NET)

## When to Invoke
- Viết feature mới
- Fix bug (Prove-It pattern)
- Trước bất kỳ implementation nào

---

## Core Cycle: RED-GREEN-REFACTOR

### RED — Viết failing test trước

```csharp
[Fact]
public async Task CreateAsync_ShouldReturnBookId_WhenTitleIsUnique()
{
    // Arrange
    var request = new CreateBookRequest { Title = "Clean Code", Price = 150_000m };
    _mockRepo.Setup(r => r.ExistsByTitleAsync(request.Title, default)).ReturnsAsync(false);

    // Act
    var result = await _sut.CreateAsync(request);

    // Assert
    Assert.True(result.IsSuccess);
    Assert.NotEqual(Guid.Empty, result.Value);
}
```

`dotnet test` → **FAIL**

### GREEN — Minimal code để pass

```csharp
public async Task<Result<Guid>> CreateAsync(CreateBookRequest req, CancellationToken ct = default)
{
    if (await _repo.ExistsByTitleAsync(req.Title, ct))
        return BookErrors.TitleExists;

    var book = Book.Create(req.Title, req.Price, req.CategoryId);
    _repo.Add(book);
    await _uow.SaveChangesAsync(ct);
    return book.Id;
}
```

`dotnet test` → **PASS**

### REFACTOR — Dọn dẹp, test vẫn xanh

- Naming rõ hơn?
- Error code đúng format `{Module}.{Action}`?
- Dependency rule vi phạm không?

`dotnet test` → **vẫn PASS**

---

## Prove-It Pattern (Bug Fixes)

### Step 1: Viết test reproduce bug (phải FAIL)

```csharp
[Fact]
public void Cancel_ShouldFail_WhenOrderIsDelivered_Regression()
{
    // Bug: Cancel không guard Delivered status
    var order = CreateOrderWithStatus(OrderStatus.Delivered);
    var result = order.Cancel();
    Assert.False(result.IsSuccess);              // FAIL nếu bug còn tồn tại
    Assert.Equal("Order.CannotCancel", result.Error.Code);
}
```

### Step 2: Verify FAIL → bug confirmed
### Step 3: Fix bug trong Domain method
### Step 4: Verify PASS → fix confirmed
### Step 5: Full suite

```bash
dotnet test tests/BookStore.UnitTests
```

---

## Test Pyramid (BookStore)

```
      [Integration]  ~10%  — API + DB thực (chưa ưu tiên)
    [Unit Tests]     ~90%  — Service + Domain (mock repository)
```

**Ưu tiên unit test:**
1. Order state machine (valid/invalid transitions)
2. Result Pattern (Success / Failure paths)
3. Business rule trong Service (mock IRepository)
4. Domain Invariants

---

## AAA Pattern — bắt buộc

```csharp
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

---

## [Theory] cho State Machine

```csharp
[Theory]
[InlineData(OrderStatus.Shipped)]
[InlineData(OrderStatus.Delivered)]
public void Cancel_ShouldFail_WhenNonCancellable(OrderStatus status)
{
    var order = CreateOrderWithStatus(status);
    var result = order.Cancel();
    Assert.False(result.IsSuccess);
    Assert.Equal("Order.CannotCancel", result.Error.Code);
}

[Theory]
[InlineData(OrderStatus.Pending)]
[InlineData(OrderStatus.Confirmed)]
public void Cancel_ShouldSucceed_WhenCancellable(OrderStatus status)
{
    var order = CreateOrderWithStatus(status);
    var result = order.Cancel();
    Assert.True(result.IsSuccess);
    Assert.Equal(OrderStatus.Cancelled, order.Status);
}
```

---

## Test Doubles — Moq

```csharp
// ✅ Mock ở boundary — IRepository
_mockRepo.Setup(r => r.GetByIdAsync(id, default)).ReturnsAsync(book);
_mockRepo.Setup(r => r.ExistsByTitleAsync(title, default)).ReturnsAsync(false);
_mockRepo.Verify(r => r.Add(It.IsAny<Book>()), Times.Once);

// ❌ Không mock Service khác (test tầng sai)
var mockBookService = new Mock<IBookService>(); // sai
```

---

## Naming Convention

```
{Method}_Should{Expected}_When{Condition}

✅ GetByIdAsync_ShouldReturnBookDto_WhenBookExists
✅ CreateAsync_ShouldFail_WhenTitleAlreadyExists
✅ Cancel_ShouldFail_WhenOrderIsDelivered
✅ Cancel_ShouldSucceed_WhenOrderIsPending

❌ Test_Works
❌ CreateBook_Test
❌ HandlesError
```

---

## Anti-Patterns

| Pattern | Vấn đề | Fix |
|---------|--------|-----|
| Test implementation detail | Break khi refactor | Test Result / behavior |
| Không check `Error.Code` | Bỏ sót sai lỗi | Assert cả `ErrorType` + `Code` |
| Không test Failure path | Coverage giả | Mỗi method ≥ 1 Failure test |
| Shared mock state | Test ảnh hưởng nhau | `new Mock<>()` trong constructor |
| Mock Service thay vì Repository | Sai tầng test | Chỉ mock boundary (IRepository) |

---

## Checklist

```markdown
- [ ] Test viết trước implementation (RED trước)
- [ ] Bug fix có reproduction test trước khi fix
- [ ] Mỗi Service method: test Success + Failure
- [ ] State machine: [Theory] + [InlineData] mọi transition
- [ ] Assert cả IsSuccess, ErrorType, Error.Code
- [ ] Test name: Method_ShouldX_WhenY
- [ ] dotnet test pass
```
