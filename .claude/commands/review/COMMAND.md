---
name: review
description: Five-axis code review cho BookStore — Clean Architecture, Result Pattern, Security, Performance
---

# /review — Code Review

## Usage
"Review [file / feature / PR]" hoặc "Do a code review of [changes]"

## Five-Axis Checklist

### 1. Correctness
- [ ] Result Pattern đúng — Service trả `Result<T>`, không throw exception cho lỗi nghiệp vụ
- [ ] Controller chỉ gọi `result.ToActionResult()`, không tự set status code
- [ ] Null check đầy đủ trước khi access value
- [ ] Order state machine — mọi invalid transition đều được guard
- [ ] Unit test cover cả Success và Failure path
- [ ] Xem: `.claude/rules/error-handling/RULE.md`

### 2. Readability & Simplicity
- [ ] Naming đúng convention: `{Module}Errors`, `Get{Resource}sQuery`, `_camelCase` field
- [ ] Error code format: `"{Module}.{Action}"` (vd: `"Book.NotFound"`)
- [ ] AAA comment rõ trong test (`// Arrange / // Act / // Assert`)
- [ ] Controller đủ mỏng — không chứa business logic
- [ ] Xem: `.claude/rules/naming-conventions/RULE.md`

### 3. Architecture — Clean Architecture Rules
- [ ] **Domain** không import Application / Infrastructure / API / EF Core
- [ ] **Shared** không import bất kỳ tầng nào khác
- [ ] **Application** chỉ import Domain + Shared (interface, không implementation)
- [ ] Interface repository định nghĩa trong Application, implementation trong Infrastructure
- [ ] `SaveChangesAsync` gọi ở Service, không trong Repository
- [ ] Validator đặt đúng: `Application/{Module}/Validators/`
- [ ] Error class đặt đúng: `Application/{Module}/{Module}Errors.cs`
- [ ] Xem: `.claude/rules/project-structure/RULE.md`

### 4. Security
- [ ] Endpoint cần auth có `[Authorize]`, đúng role (Admin / Customer)
- [ ] JWT validate đúng — issuer, audience, expiry
- [ ] Password dùng BCrypt, không log hash
- [ ] MinIO presigned URL có expiry
- [ ] Không có secret hardcode trong code
- [ ] Input validate qua FluentValidation trước khi xuống Service
- [ ] Xem: `.claude/rules/security/RULE.md`

### 5. Performance
- [ ] Không có N+1 — `Include()` rõ ràng
- [ ] `AsNoTracking()` trên read-only query
- [ ] `Select()` projection khi không cần full entity
- [ ] `PageSize` giới hạn tối đa 50
- [ ] Không `ToList()` rồi filter in-memory
- [ ] Xem: `.claude/rules/database/RULE.md`

---

## BookStore-Specific Red Flags

```csharp
// 🔴 Business logic trong Controller
if (await _context.Books.AnyAsync(b => b.Title == req.Title)) return Conflict();

// 🔴 throw exception cho lỗi nghiệp vụ
throw new NotFoundException($"Book {id} not found");

// 🔴 Không dùng ToActionResult()
if (!result.IsSuccess) return NotFound(result.Error.Description);

// 🔴 N+1
foreach (var book in books) book.Category = await _context.Categories.FindAsync(book.CategoryId);

// 🟡 Không giới hạn PageSize
var books = await _context.Books.Skip(0).Take(request.PageSize).ToListAsync();

// 🟡 Thiếu AsNoTracking trên read query
var books = await _context.Books.Where(...).ToListAsync(); // nên thêm .AsNoTracking()
```

---

## Output Format

| Icon | Ý nghĩa |
|------|---------|
| 🔴 **Critical** | Phải fix trước khi merge |
| 🟡 **Warning** | Nên fix, có thể block |
| 🟢 **Suggestion** | Nice to have |
| ✅ **Good** | Highlight điểm tốt |

```markdown
## Review: [File / Feature]

🔴 Critical
- BookService.CreateAsync: throw exception thay vì Result.Failure — vi phạm Result Pattern

🟡 Warning
- BooksController: thiếu [Authorize] trên endpoint DELETE

🟢 Suggestion
- GetBooksQuery: thêm AsNoTracking() để tối ưu read query

✅ Good
- BookErrors.cs tổ chức đúng, error code format chuẩn
- Unit test cover đủ Success + Failure path
```
