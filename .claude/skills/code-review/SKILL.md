---
name: Code Review & Quality
description: Five-axis review cho BookStore — Clean Architecture, Result Pattern, EF Core, Security
---

# Code Review & Quality Skill — BookStore (.NET)

> "Approve a change when it definitely improves overall code health, even if it isn't perfect."

---

## Five-Axis Review Framework

### Axis 1: Correctness

- Implementation đúng với spec / acceptance criteria?
- Result Pattern đúng — Service trả `Result<T>`, không `throw` cho lỗi nghiệp vụ?
- Controller chỉ gọi `result.ToActionResult()`?
- Null check đủ trước khi access `.Value`?
- Order state machine — mọi invalid transition đều được guard?
- Unit test cover cả **Success** và **Failure** path?
- Assert đủ 3 level: `IsSuccess` + `ErrorType` + `Error.Code`?

### Axis 2: Readability & Simplicity

- Engineer khác đọc hiểu không cần giải thích?
- Naming đúng convention: `{Module}Errors`, `_camelCase`, `Get{Resource}sQuery`?
- Error code format: `"{Module}.{Action}"` (vd: `"Book.NotFound"`)?
- Controller đủ mỏng — không chứa business logic?
- Nesting > 3 levels? Function > 30 lines?
- AAA comment rõ trong test?

### Axis 3: Architecture — Clean Architecture

- **Domain** không import Application / Infrastructure / EF Core?
- **Shared** không import bất kỳ tầng nào?
- **Application** chỉ import Domain + Shared?
- Interface repository ở Application, implementation ở Infrastructure?
- `SaveChangesAsync` gọi ở Service, không Repository?
- Validator ở `Application/{Module}/Validators/`?
- Error class ở `Application/{Module}/{Module}Errors.cs`?
- Code duplication cần extract vào Shared không?

### Axis 4: Security

- Endpoint cần auth có `[Authorize]`, đúng role (Admin/Customer)?
- JWT validate đúng — issuer, audience, expiry?
- BCrypt hash password, không log hash?
- MinIO presigned URL có expiry?
- Secret không hardcode trong code?
- FluentValidation chặn input trước khi xuống Service?
- Resource ownership được verify (user chỉ access data của mình)?

### Axis 5: Performance

- N+1 query? `Include()` đã đủ?
- `AsNoTracking()` trên read-only query?
- `Select()` projection khi không cần full entity?
- `PageSize` giới hạn tối đa 50?
- Không `ToList()` rồi filter in-memory?
- Index thiếu cho query thường xuyên dùng?

---

## BookStore Red Flags (Instant Block)

```csharp
// 🔴 Business logic trong Controller
if (await _context.Books.AnyAsync(b => b.Title == req.Title)) return Conflict();

// 🔴 throw exception cho lỗi nghiệp vụ
throw new NotFoundException($"Book {id} not found");

// 🔴 Dependency rule vi phạm
// Domain/Books/Book.cs
using BookStore.Infrastructure.Persistence; // ❌

// 🔴 N+1
foreach (var book in books)
    book.Category = await _context.Categories.FindAsync(book.CategoryId);

// 🟡 Không giới hạn PageSize
.Take(request.PageSize) // không có Max(1, Math.Min(pageSize, 50))

// 🟡 Read query không có AsNoTracking
await _context.Books.Where(...).ToListAsync(); // thiếu .AsNoTracking()
```

---

## Comment Severity Labels

| Label | Ý nghĩa | Action |
|-------|---------|--------|
| `Critical:` | Merge blocker | Bắt buộc fix |
| `Important:` | Nên fix | Có thể block |
| `Nit:` | Style / minor | Optional |
| `Optional:` | Gợi ý | Không bắt buộc |
| `FYI:` | Thông tin | Không cần action |

**Ví dụ:**
```
Critical: Service throw NotFoundException thay vì Result.Failure — vi phạm Result Pattern.

Important: Endpoint DELETE /api/books/{id} thiếu [Authorize(Roles = "Admin")].

Nit: Đổi tên biến `data` → `bookDto` để rõ hơn.

FYI: QueryableExtensions.ToPagedResultAsync() đã handle Skip/Take — không cần viết thủ công.
```

---

## Review Process

### Step 1: Hiểu context
- PR đang solve vấn đề gì?
- Có spec / acceptance criteria không?

### Step 2: Đọc test trước
- Test reveal expected behavior
- Failure path có được test không?
- `[Theory]` cho state machine có đủ case không?

### Step 3: Review implementation theo 5 axis

### Step 4: Phân loại findings
- **Must fix** — Correctness, security, dependency rule vi phạm
- **Should fix** — Readability, architecture concern
- **Consider** — Suggestions, style

### Step 5: Feedback actionable

```
❌ "Service này phức tạp quá"

✅ "BookService.CreateAsync đang throw exception thay vì trả Result.Failure(BookErrors.NotFound(id)).
    Sửa lại để đúng Result Pattern — xem Application/Books/BookErrors.cs"
```

---

## Review Output Format

```markdown
## Review: [Feature / File]

**Verdict**: APPROVE | REQUEST CHANGES | NEEDS DISCUSSION

### Critical (merge blocker)
- `BookService.cs:45` — throw exception thay vì Result.Failure

### Important
- `BooksController.cs:30` — thiếu [Authorize] trên DELETE endpoint

### Suggestions
- `BookRepository.cs:22` — thêm .AsNoTracking() cho read query

### Positives
- BookErrors.cs tổ chức đúng, error code format chuẩn
- Unit test cover đủ Success + Failure, có [Theory] cho state machine
```

---

## Rules
- Không chấp nhận "sẽ clean sau" — sẽ không bao giờ xảy ra
- Xóa dead code — không comment out
- Feedback cụ thể: file + line + suggested fix
- Honest nhưng constructive
