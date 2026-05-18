# Feature: Authors Module

## Objective
Xây dựng CRUD đầy đủ cho tác giả (Author) bao gồm upload avatar qua MinIO và mapping quan hệ N-N với Books thông qua join entity `BookAuthor` tường minh bằng Fluent API (không dùng EF Core shortcut `.UsingEntity(j => ...)`).

## Module
Authors (quan hệ N-N với Books)

---

## Trạng thái hiện tại (As-Is)

| Artifact | Trạng thái |
|----------|-----------|
| `Author.cs` entity | ✅ Đã có — `FullName`, `Bio`, `AvatarUrl`, `SetAvatar()`. Navigation hiện là `ICollection<Book> Books` (skip nav, shortcut EF) |
| `Book.cs` entity | ✅ Đã có — Navigation hiện là `ICollection<Author> Authors` (skip nav) |
| `BookConfiguration.cs` | ⚠️ Dùng shortcut `.UsingEntity(j => j.ToTable("BookAuthors"))` — cần thay bằng explicit join entity |
| `AuthorConfiguration.cs` | ✅ Đã có — đủ constraints |
| `AuthorErrors.cs` | ❌ Chưa có |
| `BookAuthor.cs` (join entity) | ❌ Chưa có |
| `BookAuthorConfiguration.cs` | ❌ Chưa có |
| `IAuthorRepository.cs` | ❌ Chưa có |
| `AuthorRepository.cs` | ❌ Chưa có |
| Application layer (Authors/) | ❌ Chưa có |
| `AuthorsController.cs` | ❌ Chưa có |
| Validators | ❌ Chưa có |
| Unit tests | ❌ Chưa có |

---

## Core Features

### 1. CRUD Author
**Acceptance criteria:**
- Admin có thể tạo / cập nhật / xóa tác giả
- User (anonymous) có thể xem danh sách và chi tiết tác giả
- Tìm kiếm theo `FullName` (SearchTerm), sắp xếp, phân trang
- Khi xóa Author: kiểm tra xem author có liên kết với Book không → trả lỗi `Conflict` nếu còn liên kết
- `AuthorDto` trong danh sách: không embed Books (tránh over-fetching)
- `AuthorDetailDto` trong GET by ID: embed danh sách Books (Id, Title, ISBN)

### 2. Upload Avatar (MinIO)
**Acceptance criteria:**
- Admin có thể upload ảnh `.jpg`, `.jpeg`, `.png`, `.webp`, tối đa **5 MB**
- Avatar đi qua `IMediaService.UploadAsync` với `module = "authors"` — **không** gọi trực tiếp `IMinioStorageService`
- `AvatarUrl` trong DB lưu **ObjectKey** (path) — không lưu presigned URL
- Khi GET author, generate presigned URL on-the-fly từ `AvatarUrl` (ObjectKey) qua `IMinioStorageService.GeneratePresignedUrlAsync`
- Endpoint riêng: `PATCH /api/authors/{id}/avatar` (không gộp vào PUT)
- Khi upload ảnh mới, ảnh cũ bị xóa khỏi MinIO qua `IMediaService.DeleteAsync`

> **Bucket:** Thêm `"authors": "author-avatars"` vào `MinioSettings.Buckets` trong `appsettings.json`.

### 3. Mapping N-N Book ↔ Author (Explicit Join Entity)
**Acceptance criteria:**
- `BookAuthor` là entity tường minh với `BookId`, `AuthorId` (composite PK)
- EF config trong `BookAuthorConfiguration` dùng `UsingEntity<BookAuthor>()` có đủ FK, composite PK, tên bảng
- Loại bỏ shortcut `.UsingEntity(j => j.ToTable("BookAuthors"))` trong `BookConfiguration`
- Navigation trên `Author`: thay `ICollection<Book> Books` → `ICollection<BookAuthor> BookAuthors`
- Navigation trên `Book`: thay `ICollection<Author> Authors` → `ICollection<BookAuthor> BookAuthors`
- API để gán/bỏ tác giả khỏi sách: **nằm trong Books Module** (out of scope bài này)

---

## Out of Scope
- Gán Author ↔ Book qua API (thuộc Books Module)
- Author search by Book (cross-module query)
- Soft delete Author (Author dùng hard delete có guard check)
- Presigned URL generation service (lưu ObjectKey, generate URL on-the-fly)

---

## Technical Approach

### Domain Layer

**Entity đã có — cần cập nhật `Author.cs`:**
```csharp
// BookStore.Domain/Entities/Author.cs
// Thay ICollection<Book> Books thành:
public ICollection<BookAuthor> BookAuthors { get; private set; } = [];
```

**Entity đã có — cần cập nhật `Book.cs`:**
```csharp
// BookStore.Domain/Entities/Book.cs
// Thay ICollection<Author> Authors thành:
public ICollection<BookAuthor> BookAuthors { get; private set; } = [];
```

**Entity mới — BookAuthor (join entity tường minh):**
```csharp
// BookStore.Domain/Entities/BookAuthor.cs
public class BookAuthor
{
    public Guid BookId { get; set; }
    public Guid AuthorId { get; set; }

    // Navigation properties
    public Book Book { get; set; } = null!;
    public Author Author { get; set; } = null!;
}
```

> **Không có** BaseEntity (không cần Id/CreatedAt/UpdatedAt trên join table).

> Khi dùng explicit join entity, skip navigations (`Book.Authors`, `Author.Books`) không còn được EF tự generate. Spec này chọn bỏ skip navigation để đơn giản, truy cập qua `author.BookAuthors.Select(ba => ba.Book)`.

**Business Invariants:**
- `FullName` không được rỗng, tối đa 150 ký tự (enforce ở FluentValidation)
- `Bio` tùy chọn, tối đa 2000 ký tự
- Xóa Author không được nếu còn liên kết với Book

**Errors — mới tạo:**
```csharp
// BookStore.Domain/Errors/AuthorErrors.cs
public static class AuthorErrors
{
    public static Error NotFound(Guid id)
        => Error.NotFound("Author.NotFound", $"Author '{id}' not found.");

    public static readonly Error FullNameExists
        = Error.Conflict("Author.FullNameExists", "An author with this name already exists.");

    public static readonly Error HasBooks
        = Error.Conflict("Author.HasBooks", "Cannot delete author because they are linked to one or more books.");

    public static readonly Error AvatarTooLarge
        = Error.Validation("Author.AvatarTooLarge", "Avatar file size must not exceed 5 MB.");

    public static readonly Error AvatarInvalidFormat
        = Error.Validation("Author.AvatarInvalidFormat", "Avatar must be a .jpg, .jpeg, .png, or .webp file.");
}
```

---

### Application Layer

**Folder structure:**
```
Application/
  Authors/
    IService/
      IAuthorQueryService.cs
      IAuthorCommandService.cs
    Services/
      AuthorQueryService.cs
      AuthorCommandService.cs
    Commands/
      CreateAuthorCommand.cs       // record: FullName, Bio?
      UpdateAuthorCommand.cs       // record: FullName, Bio?
    Queries/
      GetAuthorsQuery.cs           // extends QueryParams (SearchTerm, SortBy, IsAscending, Page, PageSize)
    DTOs/
      AuthorDto.cs                 // danh sách: Id, FullName, Bio, AvatarUrl, BookCount, CreatedAt
      AuthorDetailDto.cs           // chi tiết: thêm Books (List<AuthorBookDto>)
      AuthorBookDto.cs             // nested: Id, Title, ISBN
```

> `AuthorErrors.cs` đặt tại `BookStore.Domain/Errors/AuthorErrors.cs` theo convention (xem `CategoryErrors.cs`).

**Interfaces:**
```csharp
// IAuthorQueryService
Task<Result<PagedResult<AuthorDto>>> GetPagedAsync(GetAuthorsQuery query, CancellationToken ct = default);
Task<Result<AuthorDetailDto>> GetByIdAsync(Guid id, CancellationToken ct = default);

// IAuthorCommandService
Task<Result<Guid>> CreateAsync(CreateAuthorCommand command, CancellationToken ct = default);
Task<Result> UpdateAsync(Guid id, UpdateAuthorCommand command, CancellationToken ct = default);
Task<Result> DeleteAsync(Guid id, CancellationToken ct = default);
Task<Result<string>> UploadAvatarAsync(Guid id, IFormFile file, Guid uploadedBy, CancellationToken ct = default);
```

**Repository interface (Domain layer):**
```csharp
// BookStore.Domain/IRepository/IAuthorRepository.cs
Task<Author?> GetByIdAsync(Guid id, CancellationToken ct = default);
Task<Author?> GetByIdWithBooksAsync(Guid id, CancellationToken ct = default);
Task<bool> ExistsByFullNameAsync(string fullName, CancellationToken ct = default);
Task<bool> HasBooksAsync(Guid id, CancellationToken ct = default);
IQueryable<Author> GetQueryable();
void Add(Author author);
void Remove(Author author);
```

**Storage abstraction — dùng lại interface đã có:**
```csharp
// BookStore.Application/Media/IService/IMinioStorageService.cs — ĐÃ CÓ
Task UploadAsync(string bucketName, string objectKey, Stream stream, string contentType, long size, CancellationToken ct);
Task DeleteAsync(string bucketName, string objectKey, CancellationToken ct);
Task<string> GeneratePresignedUrlAsync(string bucketName, string objectKey, int expirySeconds);
Task EnsureBucketsAsync(IEnumerable<string> bucketNames, CancellationToken ct);

// BookStore.Application/Media/IService/IMediaService.cs — ĐÃ CÓ
Task<Result<MediaDto>> UploadAsync(UploadMediaCommand cmd, CancellationToken ct = default);
Task<Result> DeleteAsync(Guid mediaId, Guid requestingUserId, bool isAdmin, CancellationToken ct = default);
```

> `AuthorCommandService` inject `IMediaService` (không inject `IMinioStorageService` trực tiếp).
> `AuthorQueryService` inject `IMinioStorageService` để generate presigned URL khi trả `AuthorDto`/`AuthorDetailDto`.

**Avatar flow — trong `AuthorCommandService`:**
```
UploadAvatarAsync:
  1. Load author, NotFound nếu không tồn tại
  2. Validate file (size <= 5 MB, extension hợp lệ) → trả lỗi tương ứng
  3. Nếu AvatarUrl (ObjectKey) đang tồn tại → lookup Media record → gọi _mediaService.DeleteAsync
  4. Gọi _mediaService.UploadAsync(UploadMediaCommand { File, Module = "authors", UploadedBy })
  5. Gọi author.SetAvatar(mediaResult.Value.ObjectKey)
  6. SaveChangesAsync → trả presigned URL từ MediaDto.Url
```

---

### Infrastructure Layer

**BookAuthor EF Configuration (mới):**
```csharp
// BookStore.Infrastructure/Data/Configurations/BookAuthorConfiguration.cs
public class BookAuthorConfiguration : IEntityTypeConfiguration<BookAuthor>
{
    public void Configure(EntityTypeBuilder<BookAuthor> builder)
    {
        builder.ToTable("BookAuthors");
        builder.HasKey(ba => new { ba.BookId, ba.AuthorId });

        builder.HasOne(ba => ba.Book)
               .WithMany(b => b.BookAuthors)
               .HasForeignKey(ba => ba.BookId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ba => ba.Author)
               .WithMany(a => a.BookAuthors)
               .HasForeignKey(ba => ba.AuthorId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
```

**BookConfiguration — loại bỏ shortcut:**
```csharp
// Xóa dòng sau trong BookConfiguration.cs (dòng 58-60 hiện tại):
// builder.HasMany(b => b.Authors)
//        .WithMany(a => a.Books)
//        .UsingEntity(j => j.ToTable("BookAuthors"));
// BookAuthorConfiguration xử lý toàn bộ relationship
```

**MinioStorageService — đã có, không tạo mới:**
```csharp
// BookStore.Infrastructure/Storage/MinioStorageService.cs — ĐÃ CÓ
// Implement IMinioStorageService với: UploadAsync, DeleteAsync, GeneratePresignedUrlAsync, EnsureBucketsAsync
```

**Migration:**
- Bảng `BookAuthors` đã tồn tại (tạo bởi shortcut) — migration `ReplaceBookAuthorsWithExplicitJoinEntity` chỉ cần kiểm tra schema, không drop/recreate nếu schema không đổi
- Nếu EF phát hiện model change (do navigation property đổi) → tạo migration mới

---

### API Layer

**Endpoints:**

| Method | Route | Auth | Response |
|--------|-------|------|----------|
| GET | `/api/authors` | Anonymous | `200 PagedResult<AuthorDto>` |
| GET | `/api/authors/{id:guid}` | Anonymous | `200 AuthorDetailDto` / `404` |
| POST | `/api/authors` | Admin | `201 AuthorDto` |
| PUT | `/api/authors/{id:guid}` | Admin | `200` / `404` / `409` |
| DELETE | `/api/authors/{id:guid}` | Admin | `204` / `404` / `409` |
| PATCH | `/api/authors/{id:guid}/avatar` | Admin | `200 { avatarUrl }` / `400` / `404` |

**AuthorsController pattern:**
```csharp
[Route("api/authors")]
public class AuthorsController(IAuthorQueryService queryService, IAuthorCommandService commandService)
    : BaseController
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] GetAuthorsQuery query, CancellationToken ct)
        => HandlePagedResult(await queryService.GetPagedAsync(query, ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => HandleResult(await queryService.GetByIdAsync(id, ct));

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(CreateAuthorCommand command, CancellationToken ct)
        => HandleCreated(await commandService.CreateAsync(command, ct), nameof(GetById));

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(Guid id, UpdateAuthorCommand command, CancellationToken ct)
        => HandleResult(await commandService.UpdateAsync(id, command, ct));

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        => HandleResult(await commandService.DeleteAsync(id, ct));

    [HttpPatch("{id:guid}/avatar")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UploadAvatar(Guid id, IFormFile file, CancellationToken ct)
    {
        var uploadedBy = GetCurrentUserId(); // helper từ BaseController
        return HandleResult(await commandService.UploadAvatarAsync(id, file, uploadedBy, ct));
    }
}
```

---

## DTOs

```csharp
// Danh sách — không embed Books
public record AuthorDto(
    Guid Id,
    string FullName,
    string? Bio,
    string? AvatarUrl,     // presigned URL, null nếu không có avatar
    int BookCount,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

// Chi tiết — embed Books
public record AuthorDetailDto(
    Guid Id,
    string FullName,
    string? Bio,
    string? AvatarUrl,     // presigned URL, null nếu không có avatar
    List<AuthorBookDto> Books,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

// Nested trong AuthorDetailDto
public record AuthorBookDto(Guid Id, string Title, string ISBN);

// Commands
public record CreateAuthorCommand(string FullName, string? Bio);
public record UpdateAuthorCommand(string FullName, string? Bio);

// Query
public sealed class GetAuthorsQuery : QueryParams { }
// Kế thừa SearchTerm, SortBy, IsAscending, Page, PageSize từ QueryParams
```

---

## Validation Plan

| Rule | Tầng | Công cụ |
|------|------|---------|
| FullName required, max 150 chars | API | FluentValidation |
| Bio max 2000 chars | API | FluentValidation |
| FullName unique | Application | Business rule: `ExistsByFullNameAsync` |
| Avatar: max 5MB | Application | Business rule: `file.Length <= 5_242_880` |
| Avatar: valid extension | Application | Business rule: check `.jpg .jpeg .png .webp` |
| Author exists | Application | Business rule: `GetByIdAsync != null` |
| Author has no linked books | Application | Business rule: `HasBooksAsync` (before delete) |

**Validators (đặt tại `BookStore.API/Validators/`):**
- `CreateAuthorCommandValidator` — FullName NotEmpty + MaxLength(150), Bio MaxLength(2000)
- `UpdateAuthorCommandValidator` — tương tự
- File validation (size, extension) → làm trong Service (không dùng FluentValidation cho IFormFile, tái dùng pattern như `MediaService`)

---

## Testing Strategy

Test project: `BookStore.Application.Tests/Application/Authors/`

### Unit Tests — AuthorCommandService
```
CreateAsync_ShouldReturnGuid_WhenValidCommand
CreateAsync_ShouldFail_WhenFullNameAlreadyExists          → Author.FullNameExists
DeleteAsync_ShouldFail_WhenAuthorHasLinkedBooks           → Author.HasBooks
DeleteAsync_ShouldSucceed_WhenNoLinkedBooks
UpdateAsync_ShouldFail_WhenAuthorNotFound                 → Author.NotFound
UploadAvatarAsync_ShouldFail_WhenFileTooLarge             → Author.AvatarTooLarge
UploadAvatarAsync_ShouldFail_WhenInvalidFormat            → Author.AvatarInvalidFormat
UploadAvatarAsync_ShouldDeleteOldAvatar_WhenAvatarExists
UploadAvatarAsync_ShouldReturnPresignedUrl_WhenSuccess
```

### Unit Tests — AuthorQueryService
```
GetByIdAsync_ShouldReturnAuthorDetail_WhenFound
GetByIdAsync_ShouldReturnNotFound_WhenMissing             → Author.NotFound
GetPagedAsync_ShouldApplySearchTerm
GetPagedAsync_ShouldReturnEmptyWhenNoMatch
```

**Mocks cần:** `IAuthorRepository`, `IMediaService`, `IMinioStorageService`, `IUnitOfWork`

---

## Implementation Order (cho `/plan`)

1. **Domain:** `BookAuthor` entity + `AuthorErrors.cs`
2. **Domain:** Update `Author.cs` (thay `Books` → `BookAuthors`) + Update `Book.cs` (thay `Authors` → `BookAuthors`)
3. **Domain:** `IAuthorRepository.cs`
4. **Infrastructure:** `BookAuthorConfiguration.cs` + loại bỏ shortcut trong `BookConfiguration.cs`
5. **Infrastructure:** `AuthorRepository.cs` + register DI
6. **Infrastructure:** Migration `ReplaceBookAuthorsWithExplicitJoinEntity` (nếu EF phát hiện model change)
7. **Infrastructure:** Thêm bucket `"authors": "author-avatars"` vào `MinioSettings.Buckets` + `appsettings.json`
8. **Application:** DTOs (`AuthorDto`, `AuthorDetailDto`, `AuthorBookDto`) + Commands + Query
9. **Application:** `IAuthorQueryService` + `AuthorQueryService`
10. **Application:** `IAuthorCommandService` + `AuthorCommandService` (có avatar upload qua `IMediaService`)
11. **API:** Validators + `AuthorsController`
12. **Tests:** Unit tests `AuthorCommandServiceTests` + `AuthorQueryServiceTests`

---

## Boundaries

### Always Do
- Result Pattern — không throw exception cho lỗi nghiệp vụ
- Error code format: `Author.{Action}` (vd: `Author.NotFound`, `Author.HasBooks`)
- Controller chỉ gọi `HandleResult()` / `HandlePagedResult()` / `HandleCreated()`
- Validation format/required → FluentValidation; unique/exists → Service
- `SaveChangesAsync` trong Service, không trong Repository
- Upload avatar đi qua `IMediaService` (không gọi `IMinioStorageService` trực tiếp trong CommandService)
- Lưu `ObjectKey` vào `Author.AvatarUrl`, generate presigned URL on-the-fly trong QueryService
- MinIO operation thất bại → trả `Error.Failure`, không để exception bubble lên

### Ask First
- Thêm field mới vào `Author` entity
- Thêm NuGet package mới
- Thay đổi schema của bảng `Books` hoặc `Authors` hiện có
- Tăng giới hạn file size cho avatar (hiện 5 MB)

### Never Do
- Business logic trong Controller
- `new` concrete dependency trong Service
- Skip navigation (`Author.Books`) nếu không configure rõ trong EF
- Log ở Service — log tập trung tại Middleware
- Throw exception cho lỗi nghiệp vụ (notfound, conflict, validation)
- Gọi `IMinioStorageService.UploadAsync` trực tiếp trong `AuthorCommandService` — phải đi qua `IMediaService`
- Lưu presigned URL vào `Author.AvatarUrl` — URL có expire, không bền vững
