# Feature: Authors Module

## Objective
Xây dựng CRUD đầy đủ cho tác giả (Author) bao gồm upload avatar qua MinIO và mapping quan hệ N-N với Books thông qua join entity `BookAuthor` tường minh bằng Fluent API (không dùng EF Core shortcut `.UsingEntity(j => ...)`).

## Module
Authors (quan hệ N-N với Books)

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
- Avatar được lưu vào MinIO bucket `author-avatars` với object key `avatars/{authorId}/{filename}`
- Endpoint riêng: `PATCH /api/authors/{id}/avatar` (không gộp vào PUT)
- `AvatarUrl` trong DB lưu **presigned URL** hoặc **path** để generate URL khi cần
- Khi upload ảnh mới, ảnh cũ bị xóa khỏi MinIO

### 3. Mapping N-N Book ↔ Author (Explicit Join Entity)
**Acceptance criteria:**
- `BookAuthor` là entity tường minh với `BookId`, `AuthorId` (composite PK)
- EF config trong `BookAuthorConfiguration` dùng `UsingEntity<BookAuthor>()` có đủ FK, composite PK, tên bảng
- Loại bỏ shortcut `.UsingEntity(j => j.ToTable("BookAuthors"))` trong `BookConfiguration`
- API để gán/bỏ tác giả khỏi sách: **nằm trong Books Module** (out of scope bài này)

---

## Out of Scope
- Gán Author ↔ Book qua API (thuộc Books Module)
- Author search by Book (cross-module query)
- Soft delete Author (Author dùng hard delete có guard check)
- Presigned URL generation service (MinIO URL lưu object path, generate URL trong service)

---

## Technical Approach

### Domain Layer

**Entity đã có — cần bổ sung:**

```csharp
// BookStore.Domain/Entities/Author.cs — KHÔNG SỬA
// FullName, Bio, AvatarUrl, SetAvatar() đã đủ
// Thêm navigation: ICollection<BookAuthor> BookAuthors
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

**Không có** BaseEntity (không cần Id/CreatedAt/UpdatedAt trên join table).

**Business Invariants:**
- `FullName` không được rỗng, tối đa 150 ký tự (enforce ở FluentValidation)
- `Bio` tùy chọn, tối đa 2000 ký tự
- Xóa Author không được nếu còn liên kết với Book

**Errors:**
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
    AuthorErrors.cs                // đặt trong Domain, không phải đây
```

> `AuthorErrors.cs` đặt tại `BookStore.Domain/Errors/AuthorErrors.cs` theo convention (xem Categories).

**Interfaces:**

```csharp
// IAuthorQueryService
Task<Result<PagedResult<AuthorDto>>> GetPagedAsync(GetAuthorsQuery query, CancellationToken ct = default);
Task<Result<AuthorDetailDto>> GetByIdAsync(Guid id, CancellationToken ct = default);

// IAuthorCommandService
Task<Result<Guid>> CreateAsync(CreateAuthorCommand command, CancellationToken ct = default);
Task<Result> UpdateAsync(Guid id, UpdateAuthorCommand command, CancellationToken ct = default);
Task<Result> DeleteAsync(Guid id, CancellationToken ct = default);
Task<Result<string>> UploadAvatarAsync(Guid id, IFormFile file, CancellationToken ct = default);
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

**MinIO Storage abstraction:**
```csharp
// BookStore.Application/Common/IStorage/IStorageService.cs
public interface IStorageService
{
    Task<string> UploadAsync(string bucketName, string objectKey, Stream content, string contentType, CancellationToken ct = default);
    Task DeleteAsync(string bucketName, string objectKey, CancellationToken ct = default);
    string GetObjectUrl(string bucketName, string objectKey);
}
```

> Implementation `MinioStorageService` đặt tại `BookStore.Infrastructure/Storage/MinioStorageService.cs`

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
// Xóa dòng sau trong BookConfiguration.cs:
// builder.HasMany(b => b.Authors).WithMany(a => a.Books).UsingEntity(j => j.ToTable("BookAuthors"));
// BookAuthorConfiguration xử lý toàn bộ relationship
```

**Book entity — thêm navigation:**
```csharp
// Thay ICollection<Author> Authors thành:
public ICollection<BookAuthor> BookAuthors { get; private set; } = [];
// Skip navigation (vẫn query được book.Authors qua BookAuthors)
```

> Lưu ý: Khi dùng explicit join entity, skip navigations (`Book.Authors`, `Author.Books`) không còn được EF tự generate. Nếu muốn giữ, cần configure rõ ràng trong `BookAuthorConfiguration` với `HasSkipNavigation`. Spec này chọn bỏ skip navigation để đơn giản, truy cập qua `author.BookAuthors.Select(ba => ba.Book)`.

**MinioStorageService:**
```csharp
// BookStore.Infrastructure/Storage/MinioStorageService.cs
public class MinioStorageService(IMinioClient minioClient, IOptions<MinioSettings> options) : IStorageService
{
    Task<string> UploadAsync(...) // PutObjectAsync + return object URL
    Task DeleteAsync(...)         // RemoveObjectAsync
    string GetObjectUrl(...)      // construct URL từ endpoint + bucket + key
}
```

**Migration:**
- Khi `BookAuthor` thay EF shortcut → check xem migration nào đã tạo bảng `BookAuthors`
- Nếu bảng chưa tồn tại → migration mới `AddBookAuthorExplicitJoin`
- Nếu bảng đã tồn tại (từ shortcut) → migration update schema nếu schema thay đổi

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
        => HandleResult(await commandService.UploadAvatarAsync(id, file, ct));
}
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

**Validators:**
- `CreateAuthorCommandValidator` — FullName required + maxlength, Bio maxlength
- `UpdateAuthorCommandValidator` — idem
- File validation (size, extension) → làm trong Service (không dùng FluentValidation cho IFormFile)

---

## Testing Strategy

### Unit Tests — AuthorCommandService
```
CreateAsync_ShouldSucceed_WhenValidCommand
CreateAsync_ShouldFail_WhenFullNameAlreadyExists      → Author.FullNameExists
DeleteAsync_ShouldFail_WhenAuthorHasLinkedBooks       → Author.HasBooks
DeleteAsync_ShouldSucceed_WhenNoLinkedBooks
UpdateAsync_ShouldFail_WhenAuthorNotFound             → Author.NotFound
UploadAvatarAsync_ShouldFail_WhenFileTooLarge         → Author.AvatarTooLarge
UploadAvatarAsync_ShouldFail_WhenInvalidFormat        → Author.AvatarInvalidFormat
UploadAvatarAsync_ShouldDeleteOldAvatar_WhenAvatarExists
```

### Unit Tests — AuthorQueryService
```
GetByIdAsync_ShouldReturnAuthorDetail_WhenFound
GetByIdAsync_ShouldReturnNotFound_WhenMissing         → Author.NotFound
GetPagedAsync_ShouldApplySearchTerm
```

**Mocks cần:** `IAuthorRepository`, `IStorageService`, `IUnitOfWork`

---

## Implementation Order (cho `/plan`)

1. Domain: `BookAuthor` entity + `AuthorErrors`
2. Domain: Update `Author` + `Book` navigation (thêm `BookAuthors` collection)
3. Infrastructure: `BookAuthorConfiguration` + loại bỏ shortcut trong `BookConfiguration`
4. Infrastructure: `MinioStorageService` + `IStorageService`
5. Infrastructure: `AuthorRepository` + register DI
6. Application: DTOs + Commands + Queries
7. Application: `IAuthorQueryService` + `AuthorQueryService`
8. Application: `IAuthorCommandService` + `AuthorCommandService` (có avatar upload)
9. API: Validators + `AuthorsController`
10. Migration: `AddBookAuthorExplicitJoin`
11. Tests: Unit tests Service

---

## Boundaries

### Always Do
- Dùng Result Pattern — không throw exception cho lỗi nghiệp vụ
- Error code format: `Author.{Action}` (vd: `Author.NotFound`)
- Controller chỉ gọi `HandleResult()` / `HandlePagedResult()` / `HandleCreated()`
- Validation format/required → FluentValidation; unique/exists → Service
- `SaveChangesAsync` trong Service, không trong Repository
- MinIO operation thất bại → trả `Error.Failure`, không để exception bubble lên

### Ask First
- Thêm field mới vào `Author` entity
- Thêm NuGet package mới
- Thay đổi schema của bảng `Books` hoặc `Authors` hiện có

### Never Do
- Business logic trong Controller
- `new` concrete dependency trong Service
- Skip navigation (`Author.Books`) nếu không configure rõ trong EF
- Log ở Service — log tập trung tại Middleware
- Throw exception cho lỗi nghiệp vụ (notfound, conflict, validation)
