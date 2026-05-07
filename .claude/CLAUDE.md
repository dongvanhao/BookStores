# BookStore Backend — Agent Config

## Stack
ASP.NET Core Web API · EF Core + Fluent API · SQL Server · JWT · MinIO · Docker · FluentValidation · xUnit/NUnit + Moq

## Architecture
**Clean Architecture** — dependency rule nghiêm ngặt:
```
Domain ← Application ← Infrastructure
                     ← API
         Shared (độc lập, không phụ thuộc ai)
```
**Flow:** `Controller → ValidationFilter → Service → Domain → Repository → DB`

## Workflow
`/spec` → `/plan` → `/build` → `/test` → `/review` → `/deploy`
- `/debug` — root cause analysis
- `/simplify` — giảm complexity, không đổi behavior
- `/fix-issue` — phân tích và fix issue

## Mandatory Rules

### 0. SOLID — BẮT BUỘC, áp dụng cho mọi class được generate
> Chi tiết đầy đủ: `.claude/rules/solid-principles/RULE.md`

| Nguyên tắc | Quy tắc cốt lõi | Vi phạm phổ biến cần tránh |
|------------|-----------------|---------------------------|
| **SRP** | Mỗi class 1 lý do thay đổi | Service vừa business logic vừa log/email |
| **OCP** | Mở rộng bằng class mới, không sửa code cũ | `switch` on type thay vì Strategy pattern |
| **LSP** | Implementation thay thế được interface mà không break | Override method để throw `NotSupportedException` |
| **ISP** | Interface tối đa 5–6 method; tách Query/Command nếu cần | Interface "toàn năng" có method consumer không dùng |
| **DIP** | Inject qua constructor, phụ thuộc abstraction | `new` dependency trong class, dùng `ServiceLocator` |

**Checklist trước khi hoàn thành bất kỳ class nào:**
- [ ] SRP: class chỉ có 1 lý do thay đổi?
- [ ] OCP: thêm behavior không cần sửa code cũ?
- [ ] LSP: implementation không override để ném exception?
- [ ] ISP: interface không có method thừa với consumer?
- [ ] DIP: không `new` dependency, không phụ thuộc concrete class?

### 1. Result Pattern — BẮT BUỘC, không throw exception cho lỗi nghiệp vụ
```csharp
// Service trả về Result
if (book is null) return BookErrors.NotFound(id); // implicit conversion
return book.ToDto();

// Error tập trung theo module
// Application/Books/BookErrors.cs
public static class BookErrors
{
    public static Error NotFound(Guid id) => Error.NotFound("Book.NotFound", $"Book '{id}' not found.");
    public static readonly Error TitleExists = Error.Conflict("Book.TitleExists", "Title already exists.");
}

// Controller: 1 dòng duy nhất
return result.ToActionResult(); // tự map ErrorType → HTTP status
```

### 2. Validation — đúng tầng, đúng trách nhiệm
| Tầng | Công cụ | Validate gì |
|------|---------|-------------|
| API | FluentValidation (auto-validation) | Format, required, length, regex — không cần query DB |
| Application | Business rule trong Service, trả `Result.Failure(error)` | Ràng buộc cần query DB (unique, exists, ...) |
| Domain | Private ctor + factory method | Invariants, state machine |

**FluentValidation — quy tắc bắt buộc:**
- Mỗi rule có `.WithMessage("...")`, kết thúc bằng dấu chấm
- Không dùng `.Must()` cho rule đã có sẵn (`NotEmpty` đã cover `IsNullOrWhiteSpace`)
- Không dùng `.Transform()` — trimming thuộc model binding
- Validator đặt trong `BookStore.API/Validators/`, tên file `{Command}Validator.cs`
- Đăng ký tự động qua `AddValidatorsFromAssemblyContaining<>` trong `ServiceExtensions`

### 3. Paging — dùng Shared abstractions
```csharp
public sealed class GetBooksQuery : QueryParams // kế thừa SearchTerm, SortBy, IsAscending, Page, PageSize
{
    public Guid? CategoryId { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
}

// Repository — 1 dòng thay cho boilerplate
return await query.ApplySort(request.SortBy, request.IsAscending)
                  .ToPagedResultAsync(request, ct);
```

### 4. ApiResponse — không trả object thô
```csharp
return ApiResponse<BookDto>.Ok(dto);
return ApiResponse<BookDto>.Fail("message", "Error.Code");
```

### 5. Async/Await
```csharp
// Luôn có CancellationToken
Task<Result<BookDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
// Không dùng .Result / .Wait()
```

### 6. EF Core — tránh N+1
```csharp
// Eager loading rõ ràng
var books = await _context.Books
    .Include(b => b.Category)
    .Where(b => !b.IsDeleted)
    .ToListAsync(ct);
```

## Application Layer — Cấu trúc module
Mỗi module tổ chức theo pattern sau (ví dụ module `Auth`):
```
Application/
  Auth/
    IService/                    ← Interface của service
      IAuthService.cs
    Services/                    ← Implementation của service
      AuthService.cs
    Commands/                    ← Input: RegisterCommand, LoginCommand, ...
    DTOs/                        ← Output: AuthResponse, UserDto, ...
    AuthErrors.cs                ← Lỗi tập trung theo module
    JwtOptions.cs (nếu cần)      ← Config POCO riêng của module
```

**Quy tắc bắt buộc:**
- Interface luôn trong `{Module}/IService/`
- Implementation luôn trong `{Module}/Services/`
- Namespace theo folder: `BookStore.Application.Auth.IService`, `BookStore.Application.Auth.Services`

## Naming
| Thành phần | Pattern | Ví dụ |
|------------|---------|-------|
| Entity | PascalCase | `Book`, `Order` |
| DTO | PascalCase + suffix | `BookDto`, `CreateBookRequest` |
| Error class | `{Module}Errors` | `BookErrors`, `OrderErrors` |
| Query/Command | Descriptive + suffix | `GetBooksQuery`, `CreateBookCommand` |
| Validator | `{Command}Validator` | `CreateBookCommandValidator` |
| Repository | `I{Entity}Repository` | `IBookRepository` |
| Service interface | `I{Module}Service` trong `{Module}/IService/` | `IAuthService`, `IBookService` |
| Service implementation | `{Module}Service` trong `{Module}/Services/` | `AuthService`, `BookService` |

## Modules
| Module | Tính năng |
|--------|-----------|
| Auth | Register, Login, JWT (Access+Refresh), BCrypt, Admin/Customer roles |
| Books | CRUD, MinIO image upload, filter/sort/paging, soft delete |
| Authors | CRUD, avatar upload, many-many Books |
| Categories | Đa cấp (Parent–Child) |
| Orders | Giỏ hàng, state machine: Pending→Confirmed→Shipped→Delivered / Cancelled |
| Reviews | Rating 1–5, 1 user/1 sách/1 review |
| Dashboard | Doanh thu, top sách, đơn hàng mới (Admin) |

## Test Strategy
**Scope:** Application + Domain logic | **Tools:** xUnit/NUnit + Moq

Ưu tiên:
1. Order state machine (valid/invalid transitions)
2. Result Pattern (Success/Failure paths)
3. Business rule validation (mock repository)
4. Auth logic (hash, token)

## Five-Axis Review (`/review`)
| Axis | Kiểm tra |
|------|----------|
| Correctness | Result Pattern đúng? Validation đúng tầng? |
| Readability | Error class tập trung? Naming rõ? |
| Architecture | Dependency rule? Shared không import tầng khác? SOLID tuân thủ? |
| Security | JWT validate đúng? BCrypt? MinIO URL có expire? |
| Performance | N+1? PageSize ≤ 50? Include cần thiết? |

**SOLID checks trong Architecture axis:**
- SRP: Service không chứa log/email/notification trực tiếp?
- OCP: Domain logic không dùng `switch-on-type` → Strategy pattern?
- LSP: Không có override ném `NotSupportedException`?
- ISP: Interface ≤ 5–6 method, tách Query/Command nếu cần?
- DIP: Không `new` dependency, không inject `IServiceProvider` vào Service?

## Agent Guidelines
1. Không throw exception cho lỗi nghiệp vụ — dùng Result Pattern
2. Validation đúng tầng (API/Application/Domain)
3. TDD — viết failing test trước khi implement
4. Commit nhỏ, luôn buildable
5. Giải thích trước khi làm
6. Fix root cause, không patch symptom
7. Dependency rule nghiêm ngặt — Domain không biết tầng nào khác
8. SOLID checklist bắt buộc trước khi hoàn thành bất kỳ class nào (xem Rule 0)
9. **Không tự ý commit hoặc push** — chỉ thực hiện khi user yêu cầu tường minh (xem `.claude/rules/git-workflow/RULE.md`)
