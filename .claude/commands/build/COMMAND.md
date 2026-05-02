---
name: build
description: Implement tasks theo TDD (RED-GREEN-REFACTOR), từng vertical slice một
---

# /build — Incremental Implementation

> "The simplest thing that could work."

## Prerequisites
- Plan đã approve (`docs/tasks/todo.md`)
- Hiểu acceptance criteria của task hiện tại

## Workflow — Cho mỗi task

### Step 1: Load Context
1. Đọc acceptance criteria của task
2. Xác định file liên quan và pattern hiện có trong codebase
3. Hiểu interface / Result type liên quan

---

### Step 2: RED — Viết failing test trước

```csharp
// BookStore.UnitTests/Application/Books/BookServiceTests.cs
[Fact]
public async Task CreateAsync_ShouldReturnBookId_WhenTitleIsUnique()
{
    // Arrange
    var command = new CreateBookRequest { Title = "Clean Code", Price = 150_000m };
    _mockRepo.Setup(r => r.ExistsByTitleAsync(command.Title, default)).ReturnsAsync(false);

    // Act
    var result = await _sut.CreateAsync(command);

    // Assert
    Assert.True(result.IsSuccess);
    Assert.NotEqual(Guid.Empty, result.Value);
}
```

Chạy test → confirm **FAIL**.

---

### Step 3: GREEN — Implementation tối thiểu

Implement theo đúng thứ tự Clean Architecture:

```
1. Domain   — Entity.Create() / Invariant method
2. Errors   — {Module}Errors.cs
3. Application — Service method (Result Pattern)
4. Infrastructure — Repository + EF Config + Migration
5. API      — Controller endpoint
```

```csharp
// Application/Books/BookService.cs — minimum để pass test
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

Chạy test → confirm **PASS**.

---

### Step 4: REFACTOR — Dọn dẹp

- Naming rõ ràng hơn không?
- Extract helper nếu cần?
- Duplicate code?
- Error code đúng format `{Module}.{Action}`?
- Dependency rule có bị vi phạm không?

Chạy test → confirm **vẫn PASS**.

---

### Step 5: Verify & Commit

```bash
# Build
dotnet build

# Test
dotnet test tests/BookStore.UnitTests

# Commit — Conventional Commits
git add .
git commit -m "feat(books): add CreateBook with title uniqueness check"
git commit -m "fix(orders): prevent cancel when order is delivered"
git commit -m "test(books): add unit test for CreateBook service"
git commit -m "refactor(shared): extract ToPagedResultAsync extension"
```

### Step 6: Đánh dấu hoàn thành

```markdown
# docs/tasks/todo.md
- [x] Task 2.3: Implement CreateBook service với Result Pattern
```

---

## Rules

| Rule | Lý do |
|------|-------|
| Viết test trước khi implement | TDD — test là acceptance criteria |
| Build phải xanh sau mỗi increment | Không để broken state |
| Chỉ sửa file trong scope của task | Không refactor code ngoài lề |
| Không tạo abstraction "cho sau này" | YAGNI |
| Mỗi commit là một atomic change | Dễ revert nếu cần |

## BookStore-Specific Checklist (mỗi task)

```markdown
- [ ] Entity dùng private ctor + factory method
- [ ] Service trả Result<T>, không throw exception
- [ ] Error định nghĩa trong {Module}Errors.cs
- [ ] Controller chỉ gọi result.ToActionResult()
- [ ] FluentValidation validator tồn tại
- [ ] EF Config + Migration đúng (nếu có Entity mới)
- [ ] Unit test cover Success + Failure path
- [ ] dotnet build không warning/error
```

## When Stuck

1. **Stop** — Không push code lỗi
2. **Diagnose** — Chạy `/debug`
3. **Fix** — Giải quyết root cause
4. **Guard** — Thêm test để prevent recurrence
5. **Resume** — Tiếp tục từ điểm dừng

## Red Flags

Dừng lại nếu:
- Viết > 100 dòng mà chưa có test
- Mix nhiều task không liên quan trong 1 commit
- Scope bị mở rộng giữa chừng
- `dotnet build` fail giữa các increment
- Tạo abstraction chưa cần thiết

## Output
- Code đúng Clean Architecture, có test
- `docs/tasks/todo.md` được update
- Git history atomic, rõ ràng

## Next Step
Sau khi tất cả tasks hoàn thành → chạy `/review`.
