# Feature: Books Module (Core)

## Objective
Build the core Books module covering full CRUD with soft delete, MinIO cover image upload, and a clean query pipeline with search, filter, dynamic sort, and pagination — the primary catalog API for the BookStore.

## Module
**Books** — depends on: Categories (FK), Authors (many-many via BookAuthor), Media (cover upload)

---

## Core Features

### 1. CRUD
| Operation | Acceptance Criteria |
|-----------|---------------------|
| Create | Book saved with valid CategoryId; ISBN globally unique; price > 0; stock ≥ 0; returns `Guid` of new book |
| GetById | Returns `BookDetailDto` (cover URL resolved, category name, author list, avg rating) or `Book.NotFound` |
| GetPaged | Returns `PagedResult<BookDto>` with filters, search, sort, pagination applied |
| Update (PUT) | Replaces all mutable fields; validates same rules as Create; CategoryId must exist |
| Patch (PATCH) | Updates only provided fields; skips null; same validation per field |
| Delete (soft) | Sets `IsDeleted = true`; excluded from all queries via global filter; returns 204 |

### 2. Cover Image Upload
| Step | Acceptance Criteria |
|------|---------------------|
| Upload | POST multipart/form-data; validates content-type (image/*) and max size (5 MB); persists to MinIO bucket `book-images` |
| Store | Saves object key to `Book.CoverUrl`; old object deleted from MinIO if replaced |
| Serve | `BookDto.CoverUrl` is a presigned URL (1 h expiry) resolved at query time |

### 3. Query Pipeline
| Capability | Detail |
|------------|--------|
| Search | `SearchTerm` — full-text LIKE on `Title` and `ISBN` |
| Filter | `CategoryId` (exact); `MinPrice` / `MaxPrice` (inclusive range) |
| Sort | `SortBy` — any property name; `IsAscending` bool; powered by `System.Linq.Dynamic.Core` via `ApplySort()` |
| Pagination | `Page` / `PageSize` (max 50) via `ToPagedResultAsync()` |

---

## Out of Scope (this iteration)
- Reviews / ratings (separate module)
- Order / stock management endpoints
- Bulk create / bulk delete
- Full-text search index (SQL Server FTS) — use `LIKE` for now
- Author CRUD (Authors module — spec exists)

---

## Technical Approach

### Domain (`BookStore.Domain`)
**Entity — already exists** at [Book.cs](src/BE/Core/BookStore.Domain/Entities/Book.cs). All factory methods and domain operations are already implemented:
- `Create(title, description, isbn, publishedYear, price, stockQuantity, categoryId)`
- `Update(...)` — replaces mutable fields
- `SetCover(url)` — sets CoverUrl
- `SoftDelete()` — IsDeleted = true
- `TryReduceStock()` / `RestoreStock()` — order use-cases (out of scope here)

**Error class — create new:**
```
src/BE/Core/BookStore.Domain/Errors/BookErrors.cs
```
```csharp
public static class BookErrors
{
    public static Error NotFound(Guid id)
        => Error.NotFound("Book.NotFound", $"Book '{id}' not found.");
    public static Error ISBNExists(string isbn)
        => Error.Conflict("Book.ISBNExists", $"A book with ISBN '{isbn}' already exists.");
    public static readonly Error TitleExists
        = Error.Conflict("Book.TitleExists", "A book with this title already exists.");
    public static readonly Error InvalidCoverFile
        = Error.Validation("Book.InvalidCoverFile", "Cover must be an image file under 5 MB.");
    public static Error CategoryNotFound(Guid id)
        => Error.NotFound("Book.CategoryNotFound", $"Category '{id}' not found.");
}
```

**Repository interface — create new:**
```
src/BE/Core/BookStore.Domain/IRepository/IBookRepository.cs
```
```csharp
public interface IBookRepository
{
    Task<Book?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<bool> ExistsByTitleAsync(string title, CancellationToken ct = default);
    Task<bool> ExistsByISBNAsync(string isbn, CancellationToken ct = default);
    IQueryable<Book> GetQueryable();   // for query pipeline
    void Add(Book book);
    void Remove(Book book);
}
```

---

### Application (`BookStore.Application`)

**Module folder:**
```
Application/Books/
  BookErrors.cs            ← (moved from Domain if preferred; per convention keep in Domain)
  IService/
    IBookQueryService.cs
    IBookCommandService.cs
  Services/
    BookQueryService.cs
    BookCommandService.cs
  Commands/
    CreateBookCommand.cs
    UpdateBookCommand.cs
    PatchBookCommand.cs
  Queries/
    GetBooksQuery.cs
  DTOs/
    BookDto.cs
    BookDetailDto.cs
```

**DTOs:**
```csharp
// BookDto — used in paged list
public record BookDto(
    Guid Id,
    string Title,
    string ISBN,
    int PublishedYear,
    decimal Price,
    int StockQuantity,
    string? CoverUrl,          // presigned URL
    Guid CategoryId,
    string CategoryName,
    IReadOnlyList<string> AuthorNames,
    DateTime CreatedAt
);

// BookDetailDto — used in GetById
public record BookDetailDto(
    Guid Id,
    string Title,
    string? Description,
    string ISBN,
    int PublishedYear,
    decimal Price,
    int StockQuantity,
    string? CoverUrl,
    Guid CategoryId,
    string CategoryName,
    IReadOnlyList<AuthorSummaryDto> Authors,
    double AverageRating,
    int ReviewCount,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record AuthorSummaryDto(Guid Id, string FullName, string? AvatarUrl);
```

**Commands:**
```csharp
public record CreateBookCommand(
    string Title,
    string? Description,
    string ISBN,
    int PublishedYear,
    decimal Price,
    int StockQuantity,
    Guid CategoryId,
    IReadOnlyList<Guid> AuthorIds   // optional; empty list allowed
);

public record UpdateBookCommand(
    string Title,
    string? Description,
    string ISBN,
    int PublishedYear,
    decimal Price,
    int StockQuantity,
    Guid CategoryId,
    IReadOnlyList<Guid> AuthorIds
);

public record PatchBookCommand(
    string? Title,
    string? Description,
    string? ISBN,
    int? PublishedYear,
    decimal? Price,
    int? StockQuantity,
    Guid? CategoryId,
    IReadOnlyList<Guid>? AuthorIds
);
```

**Query:**
```csharp
public sealed class GetBooksQuery : QueryParams
{
    public Guid? CategoryId  { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
}
```

**Service Interfaces (ISP — split Query/Command):**
```csharp
// IService/IBookQueryService.cs
public interface IBookQueryService
{
    Task<Result<BookDetailDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<PagedResult<BookDto>>> GetPagedAsync(GetBooksQuery query, CancellationToken ct = default);
}

// IService/IBookCommandService.cs
public interface IBookCommandService
{
    Task<Result<Guid>>    CreateAsync(CreateBookCommand cmd, CancellationToken ct = default);
    Task<Result>          UpdateAsync(Guid id, UpdateBookCommand cmd, CancellationToken ct = default);
    Task<Result>          PatchAsync(Guid id, PatchBookCommand cmd, CancellationToken ct = default);
    Task<Result>          DeleteAsync(Guid id, CancellationToken ct = default);
    Task<Result<string>>  UploadCoverAsync(Guid id, IFormFile file, CancellationToken ct = default);
}
```

**Service implementations — key logic:**

`BookCommandService.CreateAsync`:
1. Check `ExistsByTitleAsync` → `BookErrors.TitleExists`
2. Check `ExistsByISBNAsync` → `BookErrors.ISBNExists`
3. Check `ICategoryRepository.GetByIdAsync(categoryId)` → `BookErrors.CategoryNotFound`
4. `Book.Create(...)` → `_bookRepo.Add(book)` → `_unitOfWork.SaveChangesAsync()`
5. If `AuthorIds` provided: bulk-add `BookAuthor` join rows

`BookQueryService.GetPagedAsync` (clean pipeline):
```csharp
var query = _bookRepo.GetQueryable()
    .Include(b => b.Category)
    .Include(b => b.BookAuthors).ThenInclude(ba => ba.Author);

if (request.CategoryId.HasValue)
    query = query.Where(b => b.CategoryId == request.CategoryId);
if (request.MinPrice.HasValue)
    query = query.Where(b => b.Price >= request.MinPrice);
if (request.MaxPrice.HasValue)
    query = query.Where(b => b.Price <= request.MaxPrice);
if (!string.IsNullOrWhiteSpace(request.SearchTerm))
    query = query.Where(b => b.Title.Contains(request.SearchTerm)
                          || b.ISBN.Contains(request.SearchTerm));

return await query
    .ApplySort(request.SortBy, request.IsAscending)
    .Select(b => MapToDto(b))          // project before paging to avoid over-fetch
    .ToPagedResultAsync(request, ct);
```

**MinIO cover upload flow:**
1. Validate `file.ContentType.StartsWith("image/")` and `file.Length ≤ 5_242_880`
2. Delete old object from MinIO if `book.CoverUrl` is set
3. Upload to bucket `book-images`, key = `books/{bookId}/{fileName}`
4. `book.SetCover(objectKey)` → `_unitOfWork.SaveChangesAsync()`
5. Return presigned URL (1 h) to caller

---

### Infrastructure (`BookStore.Infrastructure`)

**Repository implementation:**
```
Infrastructure/Repository/BookRepository.cs
```
- Inject `AppDbContext`
- `GetQueryable()` returns `_context.Books.AsQueryable()` (global filter applies automatically)
- `ExistsByTitleAsync` / `ExistsByISBNAsync` — `AnyAsync` queries

**Registration in DI:**
```csharp
services.AddScoped<IBookRepository, BookRepository>();
services.AddScoped<IBookQueryService, BookQueryService>();
services.AddScoped<IBookCommandService, BookCommandService>();
```

**No new migrations needed** — Book entity and BookAuthor join table already exist in schema.

---

### API (`BookStore.API`)

**Controller:** `Controllers/BooksController.cs`

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| GET | `/api/books` | Anonymous | Paged list with filters |
| GET | `/api/books/{id:guid}` | Anonymous | Book detail |
| POST | `/api/books` | Admin | Create book |
| PUT | `/api/books/{id:guid}` | Admin | Full update |
| PATCH | `/api/books/{id:guid}` | Admin | Partial update |
| DELETE | `/api/books/{id:guid}` | Admin | Soft delete |
| POST | `/api/books/{id:guid}/cover` | Admin | Upload cover image |

**Controller pattern:**
```csharp
[HttpGet]
public async Task<IActionResult> GetPaged([FromQuery] GetBooksQuery query, CancellationToken ct)
    => (await _queryService.GetPagedAsync(query, ct)).ToActionResult();

[HttpPost, Authorize(Roles = "Admin")]
public async Task<IActionResult> Create(CreateBookCommand cmd, CancellationToken ct)
    => (await _commandService.CreateAsync(cmd, ct)).ToActionResult(201);

[HttpPost("{id:guid}/cover"), Authorize(Roles = "Admin")]
public async Task<IActionResult> UploadCover(Guid id, IFormFile file, CancellationToken ct)
    => (await _commandService.UploadCoverAsync(id, file, ct)).ToActionResult();
```

**Validators (FluentValidation):**
```
API/Validators/Books/
  CreateBookCommandValidator.cs
  UpdateBookCommandValidator.cs
  PatchBookCommandValidator.cs
```

```csharp
public class CreateBookCommandValidator : AbstractValidator<CreateBookCommand>
{
    public CreateBookCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300).WithMessage("Title is required and must not exceed 300 characters.");
        RuleFor(x => x.ISBN).NotEmpty().MaximumLength(20).WithMessage("ISBN is required and must not exceed 20 characters.");
        RuleFor(x => x.PublishedYear).InclusiveBetween(1000, DateTime.UtcNow.Year).WithMessage("Published year must be a valid year.");
        RuleFor(x => x.Price).GreaterThan(0).WithMessage("Price must be greater than zero.");
        RuleFor(x => x.StockQuantity).GreaterThanOrEqualTo(0).WithMessage("Stock quantity must be non-negative.");
        RuleFor(x => x.CategoryId).NotEmpty().WithMessage("Category is required.");
    }
}
```

---

## Validation Plan

| Rule | Layer | Tool |
|------|-------|------|
| Required fields, max length, price > 0, year range | API | FluentValidation |
| ISBN uniqueness | Application | `ExistsByISBNAsync` in Service → `BookErrors.ISBNExists` |
| Title uniqueness | Application | `ExistsByTitleAsync` in Service → `BookErrors.TitleExists` |
| CategoryId exists | Application | `ICategoryRepository.GetByIdAsync` → `BookErrors.CategoryNotFound` |
| Cover file type / size | Application | Explicit check in `UploadCoverAsync` |
| ISBN format (optional) | API | Regex rule if desired |
| PageSize ≤ 50 | Shared | `PaginationParams` enforces this |

---

## Testing Strategy

### Unit Tests — `BookCommandService`
```
tests/BookStore.Application.Tests/Application/Books/BookCommandServiceTests.cs
```
| Test | Expect |
|------|--------|
| `CreateAsync` — happy path | `Result<Guid>.Success` |
| `CreateAsync` — title exists | `Result.Failure(BookErrors.TitleExists)` |
| `CreateAsync` — ISBN exists | `Result.Failure(BookErrors.ISBNExists)` |
| `CreateAsync` — category not found | `Result.Failure(BookErrors.CategoryNotFound)` |
| `UpdateAsync` — book not found | `Result.Failure(BookErrors.NotFound)` |
| `DeleteAsync` — soft deletes entity | entity `IsDeleted` = true |
| `UploadCoverAsync` — invalid file | `Result.Failure(BookErrors.InvalidCoverFile)` |

### Unit Tests — `BookQueryService`
```
tests/BookStore.Application.Tests/Application/Books/BookQueryServiceTests.cs
```
| Test | Expect |
|------|--------|
| `GetByIdAsync` — found | `Result<BookDetailDto>.Success` |
| `GetByIdAsync` — not found | `Result.Failure(BookErrors.NotFound)` |
| `GetPagedAsync` — no filters | returns paged list |
| `GetPagedAsync` — categoryId filter applied | SQL WHERE includes CategoryId |

### Mocks needed
- `Mock<IBookRepository>`
- `Mock<ICategoryRepository>`
- `Mock<IUnitOfWork>`
- `Mock<IMinioService>` (for cover upload test)

---

## Files To Create

| File | Layer |
|------|-------|
| `Domain/Errors/BookErrors.cs` | Domain |
| `Domain/IRepository/IBookRepository.cs` | Domain |
| `Infrastructure/Repository/BookRepository.cs` | Infrastructure |
| `Application/Books/IService/IBookQueryService.cs` | Application |
| `Application/Books/IService/IBookCommandService.cs` | Application |
| `Application/Books/Services/BookQueryService.cs` | Application |
| `Application/Books/Services/BookCommandService.cs` | Application |
| `Application/Books/Commands/CreateBookCommand.cs` | Application |
| `Application/Books/Commands/UpdateBookCommand.cs` | Application |
| `Application/Books/Commands/PatchBookCommand.cs` | Application |
| `Application/Books/Queries/GetBooksQuery.cs` | Application |
| `Application/Books/DTOs/BookDto.cs` | Application |
| `Application/Books/DTOs/BookDetailDto.cs` | Application |
| `API/Controllers/BooksController.cs` | API |
| `API/Validators/Books/CreateBookCommandValidator.cs` | API |
| `API/Validators/Books/UpdateBookCommandValidator.cs` | API |
| `API/Validators/Books/PatchBookCommandValidator.cs` | API |
| `Tests/Application/Books/BookCommandServiceTests.cs` | Test |
| `Tests/Application/Books/BookQueryServiceTests.cs` | Test |

---

## Boundaries

### Always Do
- Result Pattern on every service method — no thrown exceptions for business errors
- Error codes: `Book.{Action}` format
- Controller ends with `.ToActionResult()` — never set status manually
- `ApplySort` + `ToPagedResultAsync` from Shared — no manual Skip/Take
- Presigned URLs resolved at query time — never persist signed URLs
- Global soft-delete filter is active — `GetQueryable()` already excludes deleted rows

### Ask First
- Schema changes to Book entity (new required column → migration)
- New package dependencies (e.g., FluentValidation extensions)
- Changes to Shared layer abstractions

### Never Do
- Business logic in Controller
- `Book` entity referencing Application or Infrastructure
- Lazy loading — use explicit `.Include()`
- Raw SQL for standard queries — LINQ only
- `new BookRepository()` — inject via constructor DI
- Throw `NotFoundException` — return `BookErrors.NotFound(id)` instead

---

## Next Step
Run `/plan books-module` to decompose into ordered implementation tasks.
