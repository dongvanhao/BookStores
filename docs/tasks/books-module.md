# TODO: Books Module

> Spec: `docs/specs/books-module.md`
> Branch: `feature/books-module`
> Thứ tự implement: Slice 1 → 2 → 3 → 4 → 5 (mỗi slice = 1 working endpoint)

---

## Đã có sẵn — KHÔNG tạo lại

| File | Ghi chú |
|------|---------|
| `Domain/Entities/Book.cs` | `Create`, `Update`, `SetCover`, `SoftDelete` đã đủ |
| `Infrastructure/Data/Configurations/BookConfiguration.cs` | Global filter `IsDeleted` đã có |
| `Infrastructure/Data/Configurations/BookAuthorConfiguration.cs` | Join table đã cấu hình |
| `Infrastructure/Migrations/` (latest) | Schema `Books` + `BookAuthors` đã apply |
| `Application/Media/IService/IMinioStorageService.cs` | `UploadAsync`, `DeleteAsync`, `GeneratePresignedUrlAsync` dùng lại |

---

## Slice 1 — Admin tạo sách (`POST /api/books`)

> **Mục tiêu:** Admin gửi JSON → sách được lưu DB → nhận `201 Created` với `Guid` mới.
> Đây là slice nền tảng: thiết lập `BookErrors`, `IBookRepository`, `BookRepository`, `BookCommandService`, validator, DI — tất cả slice sau đều dùng lại.

### Task 1.1 — Domain foundation

**Files to create:**

- `src/BE/Core/BookStore.Domain/Errors/BookErrors.cs`
- `src/BE/Core/BookStore.Domain/IRepository/IBookRepository.cs`

**BookErrors.cs:**
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

**IBookRepository.cs:**
```csharp
public interface IBookRepository
{
    Task<Book?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<bool> ExistsByTitleAsync(string title, CancellationToken ct = default);
    Task<bool> ExistsByISBNAsync(string isbn, CancellationToken ct = default);
    IQueryable<Book> GetQueryable();
    void Add(Book book);
    void Remove(Book book);
}
```

**Acceptance criteria:**
- [ ] `BookErrors` follow format `Book.{Action}`
- [ ] `IBookRepository` nằm trong `BookStore.Domain`, không import Infrastructure

---

### Task 1.2 — Infrastructure: BookRepository

**File to create:**
- `src/BE/Core/BookStore.Infrastructure/Repository/BookRepository.cs`

```csharp
public class BookRepository(AppDbContext context) : IBookRepository
{
    public Task<Book?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => context.Books.FirstOrDefaultAsync(b => b.Id == id, ct);

    public Task<bool> ExistsByTitleAsync(string title, CancellationToken ct = default)
        => context.Books.AnyAsync(b => b.Title == title, ct);

    public Task<bool> ExistsByISBNAsync(string isbn, CancellationToken ct = default)
        => context.Books.AnyAsync(b => b.ISBN == isbn, ct);

    public IQueryable<Book> GetQueryable()
        => context.Books.AsQueryable(); // global filter IsDeleted applies

    public void Add(Book book)
        => context.Books.Add(book);

    public void Remove(Book book)
        => context.Books.Remove(book);
}
```

**Acceptance criteria:**
- [ ] `GetQueryable()` không bypass global filter `IsDeleted`
- [ ] `ExistsByTitleAsync` / `ExistsByISBNAsync` dùng `AnyAsync` (không load entity)

---

### Task 1.3 — Application: CreateBookCommand + IBookCommandService

**Files to create:**
- `src/BE/Core/BookStore.Application/Books/Commands/CreateBookCommand.cs`
- `src/BE/Core/BookStore.Application/Books/IService/IBookCommandService.cs`

**CreateBookCommand.cs:**
```csharp
public record CreateBookCommand(
    string Title,
    string? Description,
    string ISBN,
    int PublishedYear,
    decimal Price,
    int StockQuantity,
    Guid CategoryId,
    IReadOnlyList<Guid> AuthorIds
);
```

**IBookCommandService.cs** (chỉ khai báo `CreateAsync` — các method khác thêm ở slice sau):
```csharp
public interface IBookCommandService
{
    Task<Result<Guid>> CreateAsync(CreateBookCommand cmd, CancellationToken ct = default);
    Task<Result>       UpdateAsync(Guid id, UpdateBookCommand cmd, CancellationToken ct = default);
    Task<Result>       PatchAsync(Guid id, PatchBookCommand cmd, CancellationToken ct = default);
    Task<Result>       DeleteAsync(Guid id, CancellationToken ct = default);
    Task<Result<string>> UploadCoverAsync(Guid id, IFormFile file, CancellationToken ct = default);
}
```
> Khai báo toàn bộ interface ngay từ đầu để tránh sửa nhiều lần; implement từng method theo slice.

**Acceptance criteria:**
- [ ] `CreateBookCommand` là `record` (immutable input)
- [ ] `IBookCommandService` nằm trong `Application`, không biết `HttpContext`

---

### Task 1.4 — Application: BookCommandService.CreateAsync

**File to create:**
- `src/BE/Core/BookStore.Application/Books/Services/BookCommandService.cs`

**Logic `CreateAsync`:**
1. `ExistsByTitleAsync` → `BookErrors.TitleExists`
2. `ExistsByISBNAsync` → `BookErrors.ISBNExists`
3. `ICategoryRepository.GetByIdAsync(categoryId)` → `BookErrors.CategoryNotFound`
4. `Book.Create(...)` → `_bookRepo.Add(book)`
5. Nếu `AuthorIds` không rỗng → thêm `BookAuthor` rows vào DbContext
6. `_unitOfWork.SaveChangesAsync(ct)` → trả về `book.Id`

**Acceptance criteria:**
- [ ] Không throw exception — dùng `Result.Failure`
- [ ] `SaveChangesAsync` chỉ gọi 1 lần sau khi Add Book + BookAuthors
- [ ] Primary key `BookAuthor(BookId, AuthorId)` — không insert trùng

---

### Task 1.5 — API: Validator + BooksController (POST)

**Files to create:**
- `src/BE/Core/BookStore.API/Validators/Books/CreateBookCommandValidator.cs`
- `src/BE/Core/BookStore.API/Controllers/BooksController.cs`

**CreateBookCommandValidator:**
```csharp
RuleFor(x => x.Title).NotEmpty().MaximumLength(300)
    .WithMessage("Title is required and must not exceed 300 characters.");
RuleFor(x => x.ISBN).NotEmpty().MaximumLength(20)
    .WithMessage("ISBN is required and must not exceed 20 characters.");
RuleFor(x => x.PublishedYear).InclusiveBetween(1000, DateTime.UtcNow.Year)
    .WithMessage("Published year must be a valid year.");
RuleFor(x => x.Price).GreaterThan(0)
    .WithMessage("Price must be greater than zero.");
RuleFor(x => x.StockQuantity).GreaterThanOrEqualTo(0)
    .WithMessage("Stock quantity must be non-negative.");
RuleFor(x => x.CategoryId).NotEmpty()
    .WithMessage("Category is required.");
```

**BooksController.cs** (chỉ POST endpoint ở slice này):
```csharp
[Route("api/books")]
[ApiController]
public class BooksController(
    IBookCommandService commandService,
    IBookQueryService queryService) : BaseController
{
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<Guid>), 201)]
    [ProducesResponseType(typeof(ApiResponse<>), 400)]
    public async Task<IActionResult> Create(CreateBookCommand cmd, CancellationToken ct)
        => HandleResult(await commandService.CreateAsync(cmd, ct), 201);
}
```

**File to modify:**
- `src/BE/Core/BookStore.API/Extensions/ServiceExtensions.cs` — thêm DI:

```csharp
services.AddScoped<IBookRepository, BookRepository>();
services.AddScoped<IBookCommandService, BookCommandService>();
```

**Acceptance criteria:**
- [ ] `POST /api/books` không có token → 401
- [ ] `POST /api/books` với token Admin + payload hợp lệ → 201 + `{ "data": "<guid>" }`
- [ ] `POST /api/books` thiếu `Title` → 400 validation error
- [ ] `POST /api/books` trùng ISBN → 409

---

### Task 1.6 — Tests: BookCommandService.CreateAsync

**File to create:**
- `src/BE/BookStore.Application.Tests/Application/Books/BookCommandServiceTests.cs`

| Test method | Kịch bản | Expected |
|-------------|----------|----------|
| `CreateAsync_ShouldReturnBookId_WhenValid` | Happy path | `Result<Guid>.IsSuccess == true` |
| `CreateAsync_ShouldFail_WhenTitleExists` | `ExistsByTitle` → true | `Error.Code == "Book.TitleExists"` |
| `CreateAsync_ShouldFail_WhenISBNExists` | `ExistsByISBN` → true | `Error.Code == "Book.ISBNExists"` |
| `CreateAsync_ShouldFail_WhenCategoryNotFound` | `GetById(categoryId)` → null | `Error.Code == "Book.CategoryNotFound"` |

Mocks: `Mock<IBookRepository>`, `Mock<ICategoryRepository>`, `Mock<IUnitOfWork>`

**Acceptance criteria:**
- [ ] `dotnet test` pass, không có warning
- [ ] Mỗi test có comment `// Arrange / // Act / // Assert`

---

### Checkpoint Slice 1

- [ ] `dotnet build` không warning/error
- [ ] `dotnet test` pass
- [ ] Swagger: `POST /api/books` hiện với schema đúng
- [ ] Dependency rule: `BookStore.Domain` không import `Application` hay `Infrastructure`
- [ ] `BookRepository` không có business logic

---

## Slice 2 — User xem danh sách / chi tiết sách (`GET /api/books`, `GET /api/books/{id}`)

> **Mục tiêu:** Anonymous user gọi `GET /api/books` → nhận `PagedResult<BookDto>` với filter + sort + paging; gọi `GET /api/books/{id}` → nhận `BookDetailDto` đầy đủ.

### Task 2.1 — Application: DTOs + GetBooksQuery

**Files to create:**
- `src/BE/Core/BookStore.Application/Books/DTOs/BookDto.cs`
- `src/BE/Core/BookStore.Application/Books/DTOs/BookDetailDto.cs`
- `src/BE/Core/BookStore.Application/Books/Queries/GetBooksQuery.cs`

**BookDto.cs:**
```csharp
public record BookDto(
    Guid Id,
    string Title,
    string ISBN,
    int PublishedYear,
    decimal Price,
    int StockQuantity,
    string? CoverUrl,       // presigned URL hoặc null
    Guid CategoryId,
    string CategoryName,
    IReadOnlyList<string> AuthorNames,
    DateTime CreatedAt
);
```

**BookDetailDto.cs:**
```csharp
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

**GetBooksQuery.cs:**
```csharp
public sealed class GetBooksQuery : QueryParams
{
    public Guid?    CategoryId { get; set; }
    public decimal? MinPrice   { get; set; }
    public decimal? MaxPrice   { get; set; }
}
```

---

### Task 2.2 — Application: IBookQueryService + BookQueryService

**Files to create:**
- `src/BE/Core/BookStore.Application/Books/IService/IBookQueryService.cs`
- `src/BE/Core/BookStore.Application/Books/Services/BookQueryService.cs`

**IBookQueryService.cs:**
```csharp
public interface IBookQueryService
{
    Task<Result<BookDetailDto>>         GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<PagedResult<BookDto>>>  GetPagedAsync(GetBooksQuery query, CancellationToken ct = default);
}
```

**BookQueryService.GetPagedAsync — query pipeline:**
```csharp
var query = _bookRepo.GetQueryable()
    .Include(b => b.Category)
    .Include(b => b.BookAuthors).ThenInclude(ba => ba.Author);

if (request.CategoryId.HasValue)
    query = query.Where(b => b.CategoryId == request.CategoryId.Value);
if (request.MinPrice.HasValue)
    query = query.Where(b => b.Price >= request.MinPrice.Value);
if (request.MaxPrice.HasValue)
    query = query.Where(b => b.Price <= request.MaxPrice.Value);
if (!string.IsNullOrWhiteSpace(request.SearchTerm))
    query = query.Where(b => b.Title.Contains(request.SearchTerm)
                          || b.ISBN.Contains(request.SearchTerm));

return await query
    .ApplySort(request.SortBy, request.IsAscending)
    .Select(b => new BookDto(...))    // project trước khi paging
    .ToPagedResultAsync(request, ct);
```

> `CoverUrl` trong Select: nếu `b.CoverUrl != null` thì gọi `_minioService.GeneratePresignedUrlAsync(bucket, key, 3600)`.
> **Chú ý:** `GeneratePresignedUrlAsync` là `async` — không thể dùng trực tiếp trong `Select` của EF Core. Cách chuẩn: project ra `coverObjectKey` trong LINQ, rồi resolve URL sau khi `ToPagedResultAsync` trả về items.

**Acceptance criteria:**
- [ ] `GetByIdAsync` include `Category`, `BookAuthors.Author`, `Reviews` (để tính avg rating)
- [ ] `GetPagedAsync` project trước khi paging — không load toàn bộ entity
- [ ] Sort fallback: nếu `SortBy` null → sort theo `CreatedAt desc`
- [ ] Presigned URL resolve sau query (không trong LINQ tree)

**File to modify:**
- `ServiceExtensions.cs` — thêm:
```csharp
services.AddScoped<IBookQueryService, BookQueryService>();
```

---

### Task 2.3 — API: BooksController (GET endpoints)

**File to modify:**
- `src/BE/Core/BookStore.API/Controllers/BooksController.cs` — thêm:

```csharp
[HttpGet]
[AllowAnonymous]
[ProducesResponseType(typeof(ApiResponse<PagedResult<BookDto>>), 200)]
public async Task<IActionResult> GetPaged([FromQuery] GetBooksQuery query, CancellationToken ct)
    => HandlePagedResult(await queryService.GetPagedAsync(query, ct));

[HttpGet("{id:guid}")]
[AllowAnonymous]
[ProducesResponseType(typeof(ApiResponse<BookDetailDto>), 200)]
[ProducesResponseType(typeof(ApiResponse<>), 404)]
public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    => HandleResult(await queryService.GetByIdAsync(id, ct));
```

**Acceptance criteria:**
- [ ] `GET /api/books` không cần token → 200
- [ ] `GET /api/books?categoryId=...&minPrice=10&maxPrice=100&sortBy=price&isAscending=false` → filter + sort đúng
- [ ] `GET /api/books?searchTerm=clean` → tìm theo Title hoặc ISBN
- [ ] `GET /api/books/{nonexistentId}` → 404

---

### Task 2.4 — Tests: BookQueryService

**File to create:**
- `src/BE/BookStore.Application.Tests/Application/Books/BookQueryServiceTests.cs`

| Test method | Kịch bản | Expected |
|-------------|----------|----------|
| `GetByIdAsync_ShouldReturnDetail_WhenFound` | Book tồn tại | `IsSuccess = true`, `Value.Id == bookId` |
| `GetByIdAsync_ShouldReturnNotFound_WhenMissing` | Repo trả null | `Error.Code == "Book.NotFound"` |
| `GetPagedAsync_ShouldReturnAllBooks_WhenNoFilter` | Không filter | `IsSuccess = true`, items đúng |
| `GetPagedAsync_ShouldFilterByCategoryId` | `CategoryId` set | chỉ trả sách đúng category |

**Acceptance criteria:**
- [ ] `dotnet test` pass

---

### Checkpoint Slice 2

- [ ] `dotnet build` sạch
- [ ] `dotnet test` pass
- [ ] `GET /api/books` → `PagedResult` đúng format `ApiResponse<PagedResult<BookDto>>`
- [ ] `coverUrl` trong response là presigned URL (hoặc null nếu chưa upload)
- [ ] N+1: `Include` đầy đủ, không query trong vòng lặp

---

## Slice 3 — Admin cập nhật sách (`PUT /api/books/{id}`, `PATCH /api/books/{id}`)

> **Mục tiêu:** Admin PUT để replace toàn bộ fields, hoặc PATCH để update 1 phần.

### Task 3.1 — Application: UpdateBookCommand + PatchBookCommand

**Files to create:**
- `src/BE/Core/BookStore.Application/Books/Commands/UpdateBookCommand.cs`
- `src/BE/Core/BookStore.Application/Books/Commands/PatchBookCommand.cs`

```csharp
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

---

### Task 3.2 — Application: BookCommandService.UpdateAsync + PatchAsync

**File to modify:**
- `BookCommandService.cs` — implement 2 method:

**UpdateAsync logic:**
1. `GetByIdAsync(id)` → `BookErrors.NotFound`
2. Nếu `cmd.ISBN != book.ISBN`: `ExistsByISBNAsync` → `BookErrors.ISBNExists`
3. Nếu `cmd.Title != book.Title`: `ExistsByTitleAsync` → `BookErrors.TitleExists`
4. `ICategoryRepository.GetByIdAsync(cmd.CategoryId)` → `BookErrors.CategoryNotFound`
5. `book.Update(...)` — replace fields
6. Sync `BookAuthor` rows: xóa authors cũ, thêm authors mới
7. `SaveChangesAsync`

**PatchAsync logic:**
- Chỉ update các field `!= null`; bỏ qua field null
- ISBN/Title uniqueness check chỉ khi giá trị thay đổi

**Acceptance criteria:**
- [ ] Không tạo record trùng `BookAuthor` khi AuthorIds không đổi
- [ ] PATCH với `AuthorIds = null` → không thay đổi authors hiện có

---

### Task 3.3 — API: Validators + Controller (PUT/PATCH)

**Files to create:**
- `src/BE/Core/BookStore.API/Validators/Books/UpdateBookCommandValidator.cs`
- `src/BE/Core/BookStore.API/Validators/Books/PatchBookCommandValidator.cs`

**UpdateBookCommandValidator** — quy tắc giống CreateBookCommandValidator.

**PatchBookCommandValidator** — chỉ validate field khi `!= null`:
```csharp
When(x => x.Title != null, () =>
    RuleFor(x => x.Title!).MaximumLength(300).WithMessage("Title must not exceed 300 characters."));
When(x => x.Price.HasValue, () =>
    RuleFor(x => x.Price!.Value).GreaterThan(0).WithMessage("Price must be greater than zero."));
// tương tự cho các field khác
```

**File to modify:**
- `BooksController.cs` — thêm PUT + PATCH:

```csharp
[HttpPut("{id:guid}")]
[Authorize(Roles = "Admin")]
public async Task<IActionResult> Update(Guid id, UpdateBookCommand cmd, CancellationToken ct)
    => HandleResult(await commandService.UpdateAsync(id, cmd, ct));

[HttpPatch("{id:guid}")]
[Authorize(Roles = "Admin")]
public async Task<IActionResult> Patch(Guid id, PatchBookCommand cmd, CancellationToken ct)
    => HandleResult(await commandService.PatchAsync(id, cmd, ct));
```

**Acceptance criteria:**
- [ ] `PUT /api/books/{id}` với payload đầy đủ → 200
- [ ] `PUT /api/books/{nonexistentId}` → 404
- [ ] `PATCH /api/books/{id}` chỉ với `{ "price": 99.99 }` → chỉ Price thay đổi
- [ ] Trùng ISBN sau update → 409

---

### Task 3.4 — Tests: UpdateAsync + PatchAsync

**File to modify:**
- `BookCommandServiceTests.cs` — thêm:

| Test | Expected |
|------|----------|
| `UpdateAsync_ShouldSucceed_WhenValid` | `IsSuccess = true` |
| `UpdateAsync_ShouldFail_WhenBookNotFound` | `Error.Code == "Book.NotFound"` |
| `UpdateAsync_ShouldFail_WhenISBNChangedToExisting` | `Error.Code == "Book.ISBNExists"` |
| `PatchAsync_ShouldOnlyUpdateProvidedFields` | chỉ field có giá trị bị cập nhật |

---

### Checkpoint Slice 3

- [ ] `dotnet build` sạch
- [ ] `dotnet test` pass
- [ ] PUT/PATCH endpoint hoạt động đúng

---

## Slice 4 — Admin xóa sách (`DELETE /api/books/{id}`)

> **Mục tiêu:** Soft delete — `IsDeleted = true`, excluded khỏi mọi query.

### Task 4.1 — Application: BookCommandService.DeleteAsync

**File to modify:**
- `BookCommandService.cs` — implement `DeleteAsync`:

```csharp
public async Task<Result> DeleteAsync(Guid id, CancellationToken ct = default)
{
    var book = await _bookRepo.GetByIdAsync(id, ct);
    if (book is null) return Result.Failure(BookErrors.NotFound(id));

    book.SoftDelete();
    await _unitOfWork.SaveChangesAsync(ct);
    return Result.Success();
}
```

### Task 4.2 — API + Tests: DELETE endpoint

**File to modify:**
- `BooksController.cs` — thêm:

```csharp
[HttpDelete("{id:guid}")]
[Authorize(Roles = "Admin")]
[ProducesResponseType(204)]
[ProducesResponseType(typeof(ApiResponse<>), 404)]
public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    => HandleResult(await commandService.DeleteAsync(id, ct), 204);
```

**Tests:**

| Test | Expected |
|------|----------|
| `DeleteAsync_ShouldSoftDelete_WhenBookExists` | `book.IsDeleted == true`, `SaveChanges` called |
| `DeleteAsync_ShouldFail_WhenBookNotFound` | `Error.Code == "Book.NotFound"` |

**Acceptance criteria:**
- [ ] `DELETE /api/books/{id}` → 204
- [ ] Sau delete: `GET /api/books/{id}` → 404 (global filter ẩn)
- [ ] `DELETE /api/books/{nonexistentId}` → 404

---

### Checkpoint Slice 4

- [ ] `dotnet build` + `dotnet test` pass
- [ ] Global filter hoạt động: book đã delete không xuất hiện trong list

---

## Slice 5 — Admin upload ảnh bìa (`POST /api/books/{id}/cover`)

> **Mục tiêu:** Admin upload `multipart/form-data` → ảnh lưu MinIO bucket `book-images` → `Book.CoverUrl` cập nhật → response trả presigned URL.

### Task 5.1 — Application: BookCommandService.UploadCoverAsync

**File to modify:**
- `BookCommandService.cs` — implement `UploadCoverAsync`:

```csharp
public async Task<Result<string>> UploadCoverAsync(Guid id, IFormFile file, CancellationToken ct = default)
{
    // 1. Validate
    if (!file.ContentType.StartsWith("image/") || file.Length > 5_242_880)
        return Result.Failure<string>(BookErrors.InvalidCoverFile);

    // 2. Load book
    var book = await _bookRepo.GetByIdAsync(id, ct);
    if (book is null) return Result.Failure<string>(BookErrors.NotFound(id));

    // 3. Delete old cover
    if (!string.IsNullOrEmpty(book.CoverUrl))
        await _minioService.DeleteAsync("book-images", book.CoverUrl, ct);

    // 4. Upload new cover
    var objectKey = $"books/{id}/{file.FileName}";
    await using var stream = file.OpenReadStream();
    await _minioService.UploadAsync("book-images", objectKey, stream, file.ContentType, file.Length, ct);

    // 5. Persist & return URL
    book.SetCover(objectKey);
    await _unitOfWork.SaveChangesAsync(ct);

    var url = await _minioService.GeneratePresignedUrlAsync("book-images", objectKey, 3600);
    return url;
}
```

**Acceptance criteria:**
- [ ] File không phải image → 400 `Book.InvalidCoverFile`
- [ ] File > 5 MB → 400 `Book.InvalidCoverFile`
- [ ] Old object deleted trước khi upload mới
- [ ] `Book.CoverUrl` lưu `objectKey` (không phải presigned URL)

---

### Task 5.2 — API: cover upload endpoint

**File to modify:**
- `BooksController.cs` — thêm:

```csharp
/// <summary>Upload or replace the book's cover image.</summary>
[HttpPost("{id:guid}/cover")]
[Authorize(Roles = "Admin")]
[Consumes("multipart/form-data")]
[ProducesResponseType(typeof(ApiResponse<string>), 200)]
[ProducesResponseType(typeof(ApiResponse<>), 400)]
[ProducesResponseType(typeof(ApiResponse<>), 404)]
public async Task<IActionResult> UploadCover(Guid id, IFormFile file, CancellationToken ct)
    => HandleResult(await commandService.UploadCoverAsync(id, file, ct));
```

**Acceptance criteria:**
- [ ] `POST /api/books/{id}/cover` với file JPEG → 200 + presigned URL trong `data`
- [ ] Response URL có thể fetch ảnh (MinIO running locally)

---

### Task 5.3 — Tests: UploadCoverAsync

**File to modify:**
- `BookCommandServiceTests.cs` — thêm:

| Test | Expected |
|------|----------|
| `UploadCoverAsync_ShouldSucceed_WhenValidImage` | `IsSuccess = true`, URL trả về |
| `UploadCoverAsync_ShouldFail_WhenFileIsNotImage` | `Error.Code == "Book.InvalidCoverFile"` |
| `UploadCoverAsync_ShouldFail_WhenFileTooLarge` | `Error.Code == "Book.InvalidCoverFile"` |
| `UploadCoverAsync_ShouldDeleteOldCover_WhenReplacing` | `DeleteAsync` được gọi 1 lần |

Mocks: `Mock<IMinioStorageService>` thêm vào constructor.

---

### Checkpoint Slice 5 (Final)

- [ ] `dotnet build` sạch, không warning
- [ ] `dotnet test` toàn bộ pass
- [ ] Tất cả 7 endpoints hoạt động trên Swagger
- [ ] Dependency rule: Domain không import gì ngoài `Shared`
- [ ] Không có `throw` cho lỗi nghiệp vụ trong bất kỳ Service nào
- [ ] `GET /api/books` response đúng `ApiResponse<PagedResult<BookDto>>`
- [ ] Cover URL trong response là presigned, không phải object key

---

## Tổng hợp files

| File | Slice | Action |
|------|-------|--------|
| `Domain/Errors/BookErrors.cs` | 1 | CREATE |
| `Domain/IRepository/IBookRepository.cs` | 1 | CREATE |
| `Infrastructure/Repository/BookRepository.cs` | 1 | CREATE |
| `Application/Books/Commands/CreateBookCommand.cs` | 1 | CREATE |
| `Application/Books/IService/IBookCommandService.cs` | 1 | CREATE |
| `Application/Books/Services/BookCommandService.cs` | 1 | CREATE (grow qua slice 1→5) |
| `API/Validators/Books/CreateBookCommandValidator.cs` | 1 | CREATE |
| `API/Controllers/BooksController.cs` | 1 | CREATE (grow qua slice 1→5) |
| `API/Extensions/ServiceExtensions.cs` | 1+2 | MODIFY |
| `Tests/Application/Books/BookCommandServiceTests.cs` | 1 | CREATE (grow) |
| `Application/Books/DTOs/BookDto.cs` | 2 | CREATE |
| `Application/Books/DTOs/BookDetailDto.cs` | 2 | CREATE |
| `Application/Books/Queries/GetBooksQuery.cs` | 2 | CREATE |
| `Application/Books/IService/IBookQueryService.cs` | 2 | CREATE |
| `Application/Books/Services/BookQueryService.cs` | 2 | CREATE |
| `Tests/Application/Books/BookQueryServiceTests.cs` | 2 | CREATE |
| `Application/Books/Commands/UpdateBookCommand.cs` | 3 | CREATE |
| `Application/Books/Commands/PatchBookCommand.cs` | 3 | CREATE |
| `API/Validators/Books/UpdateBookCommandValidator.cs` | 3 | CREATE |
| `API/Validators/Books/PatchBookCommandValidator.cs` | 3 | CREATE |

> **Không có migration mới** — schema `Books` + `BookAuthors` đã tồn tại.
