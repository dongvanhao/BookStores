---
name: debug
description: Systematic debugging cho BookStore — tìm root cause, không patch symptom
---

# /debug — Debugging & Error Recovery

> "Fix root causes, not symptoms."

## Stop-the-Line Rule

1. **STOP** — Dừng feature work ngay
2. **PRESERVE** — Lưu error message, stack trace, log
3. **DIAGNOSE** — 6-step triage
4. **FIX** — Root cause, không symptom
5. **GUARD** — Thêm regression test
6. **RESUME** — Chỉ tiếp tục sau khi verify

---

## 6-Step Triage

### Step 1: Reproduce

```bash
# Chạy test cụ thể
dotnet test --filter "FullyQualifiedName~BookServiceTests.GetByIdAsync"

# Chạy toàn bộ
dotnet test tests/BookStore.UnitTests
```

Nếu không reproduce được → kiểm tra: state giữa các test, mock setup, seed data.

### Step 2: Localize — Tầng nào fail?

| Tầng | Triệu chứng |
|------|-------------|
| **Domain** | Factory method throw, Invariant không đúng, State machine sai |
| **Application** | Service trả sai Result, Business rule bỏ sót, Validator không trigger |
| **Infrastructure** | EF Core query sai, Migration fail, MinIO lỗi |
| **API** | Status code sai, Response không wrap ApiResponse, Filter không apply |
| **Test itself** | Mock setup sai, Assert sai expectation |

```bash
# Regression — tìm commit gây lỗi
git bisect start
git bisect bad HEAD
git bisect good <commit-hash>
```

### Step 3: Reduce — Tách nhỏ để cô lập

```csharp
// Thay vì debug cả Service method phức tạp
var result = await _bookService.CreateAsync(request, ct);

// Tách từng bước
var titleExists = await _repo.ExistsByTitleAsync(request.Title, ct);
Console.WriteLine($"[DEBUG] titleExists: {titleExists}");

var book = Book.Create(request.Title, request.Price, request.CategoryId);
Console.WriteLine($"[DEBUG] book.Id: {book.Id}");
// Xóa Console.WriteLine trước khi commit
```

### Step 4: Fix Root Cause

| Triệu chứng | Patch sai | Fix đúng |
|-------------|-----------|---------|
| Null reference trong Service | Thêm `?. ` khắp nơi | Đảm bảo Repository không trả null bất ngờ |
| Sai HTTP status code | Set status thủ công trong Controller | Fix `ErrorType` trong `Error` definition |
| N+1 query | Cache result in-memory | Thêm `Include()` đúng trong Repository |
| State machine sai | Thêm if/else patch | Guard đúng trong Domain method |
| Test flaky | Thêm delay/retry | Fix shared state giữa test — dùng `new Mock<>()` per test |

### Step 5: Guard — Regression test

```csharp
// Thêm test reproduce đúng bug trước khi fix
[Fact]
public void Cancel_ShouldFail_WhenOrderIsDelivered_Regression()
{
    // Bug: Cancel không guard OrderStatus.Delivered
    var order = CreateOrderWithStatus(OrderStatus.Delivered);
    var result = order.Cancel();
    Assert.False(result.IsSuccess);
    Assert.Equal("Order.CannotCancel", result.Error.Code);
}
```

### Step 6: Verify

```bash
dotnet build                          # Build sạch
dotnet test tests/BookStore.UnitTests # Toàn bộ test pass
dotnet test --filter "Regression"     # Regression test pass
```

---

## Error Triage Trees

### Test Failure

```
Test fail
├── Assert fail
│   ├── result.IsSuccess sai → Check Service logic / Mock setup
│   └── Error.Code sai → Check {Module}Errors.cs definition
├── NullReferenceException
│   └── Mock chưa setup → Add _mockRepo.Setup(...)
├── InvalidOperationException
│   └── DbContext chưa configure → Check test setup
└── Timeout
    └── Async không await → Kiểm tra missing await
```

### Build Error

```
dotnet build fail
├── CS error (compile)
│   ├── Type mismatch → Kiểm tra Result<T> generic type
│   ├── Missing using → Add using statement
│   └── Interface not implemented → Implement missing method
├── Package missing
│   └── dotnet restore
└── Circular dependency
    └── Kiểm tra dependency rule (Domain → Application → Infrastructure)
```

### EF Core / Runtime Error

```
Runtime error
├── EF Core
│   ├── Migration pending → dotnet ef database update
│   ├── N+1 detected → Thêm Include() hoặc Select projection
│   └── Constraint violation → Check Fluent API config + Business rule
├── MinIO
│   ├── Connection refused → Kiểm tra Docker + MINIO_ENDPOINT config
│   └── Access denied → Kiểm tra AccessKey / SecretKey
├── JWT
│   ├── 401 Unauthorized → Token expire hoặc sai Issuer/Audience
│   └── 403 Forbidden → Role claim thiếu hoặc sai
└── Result Pattern
    └── Value accessed khi IsSuccess = false → Luôn check IsSuccess trước
```

---

## Common Mistakes — BookStore

| Lỗi | Root Cause |
|-----|-----------|
| Controller trả 500 thay vì 404 | `ErrorType` không đúng trong `Error.NotFound()` |
| Test pass nhưng runtime fail | Mock khác với implementation thực tế |
| FluentValidation không trigger | Chưa đăng ký `ValidationFilter` hoặc DI sai |
| EF Include không load data | Global Query Filter chặn — `IsDeleted = true` |
| MinIO URL không access được | Presigned URL expire hoặc wrong endpoint config |

---

## Output
- Root cause xác định và fix đúng
- Regression test thêm vào
- `dotnet test` pass toàn bộ
- Commit message rõ: `fix(books): guard null when book deleted`

## Next Step
Sau khi fix → tiếp tục `/build` hoặc chạy `/review`.
