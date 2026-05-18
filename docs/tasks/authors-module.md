# TODO: Authors Module

> Spec: `docs/specs/authors-module.md`
> Branch: `feature/add-user-module`

---

## Overview — Vertical Slices

Mỗi slice deliver một tính năng hoàn chỉnh end-to-end (Domain → Infra → Application → API → Test).  
Build phải xanh sau mỗi slice.

| Slice | Feature | Phụ thuộc |
|-------|---------|-----------|
| **1** | Foundation: Explicit BookAuthor join entity + Domain wiring | — |
| **2** | Read: GET `/api/authors` + GET `/api/authors/{id}` | Slice 1 |
| **3** | Write: POST + PUT (Create/Update Author) | Slice 2 |
| **4** | Delete: DELETE với guard "HasBooks" | Slice 3 |
| **5** | Avatar: PATCH `/api/authors/{id}/avatar` | Slice 3 |

---

## Slice 1: Foundation — Explicit Join Entity + Domain

> **Objective:** Thay shortcut `.UsingEntity(j => ...)` trong `BookConfiguration` bằng explicit `BookAuthor` entity. Setup domain foundation cho toàn bộ module.

### Files to create
- `BookStore.Domain/Entities/BookAuthor.cs`
- `BookStore.Domain/Errors/AuthorErrors.cs`
- `BookStore.Domain/IRepository/IAuthorRepository.cs`
- `BookStore.Infrastructure/Data/Configurations/BookAuthorConfiguration.cs`

### Files to modify
- `BookStore.Domain/Entities/Author.cs` — đổi `ICollection<Book> Books` → `ICollection<BookAuthor> BookAuthors`
- `BookStore.Domain/Entities/Book.cs` — đổi `ICollection<Author> Authors` → `ICollection<BookAuthor> BookAuthors`
- `BookStore.Infrastructure/Data/Configurations/BookConfiguration.cs` — xóa shortcut `.HasMany(...).WithMany(...).UsingEntity(j => j.ToTable("BookAuthors"))`

### Migration
- `dotnet ef migrations add ReplaceBookAuthorsWithExplicitJoinEntity`

### Acceptance Criteria
- [x] `BookAuthor.cs` — join entity tường minh, composite PK `(BookId, AuthorId)`, navigation `Book` + `Author`
- [x] `Author.cs` — navigation đổi thành `ICollection<BookAuthor> BookAuthors { get; private set; } = []`
- [x] `Book.cs` — navigation đổi thành `ICollection<BookAuthor> BookAuthors { get; private set; } = []`
- [x] `BookAuthorConfiguration` — `HasKey(ba => new { ba.BookId, ba.AuthorId })`, FK cascade delete cả 2 chiều
- [x] `BookConfiguration` — không còn dòng `.HasMany(b => b.Authors).WithMany(...)`
- [x] `AuthorErrors.cs` — `NotFound(Guid)`, `FullNameExists`, `HasBooks`, `AvatarTooLarge`, `AvatarInvalidFormat`
- [x] `IAuthorRepository.cs` — `GetByIdAsync`, `GetByIdWithBooksAsync`, `ExistsByFullNameAsync`, `HasBooksAsync`, `GetQueryable`, `Add`, `Remove`
- [x] `dotnet build` sạch sau migration

### Verification
- [x] `dotnet build` — Domain + Infrastructure 0 error/warning
- [x] `dotnet ef migrations add` tạo migration hợp lệ (rename cột/FK, không drop/recreate table)
- [ ] `dotnet ef database update` apply thành công

---

## Checkpoint 1: Domain complete ✅

- [x] `dotnet build` sạch (Domain + Infrastructure 0 error)
- [ ] Migration apply thành công trên DB local
- [x] Dependency rule: `BookAuthor`, `AuthorErrors`, `IAuthorRepository` không import tầng Infrastructure/Application
- [x] Unit tests `AuthorEntityTests` — 9/9 PASS

---

## Slice 2: Read — GET Authors (List + Detail)

> **Objective:** User có thể xem danh sách tác giả (paged) và chi tiết tác giả (kèm Books). Avatar URL là presigned URL generate on-the-fly.

### Files to create
- `BookStore.Application/Authors/DTOs/AuthorDto.cs`
- `BookStore.Application/Authors/DTOs/AuthorDetailDto.cs`
- `BookStore.Application/Authors/DTOs/AuthorBookDto.cs`
- `BookStore.Application/Authors/Queries/GetAuthorsQuery.cs`
- `BookStore.Application/Authors/IService/IAuthorQueryService.cs`
- `BookStore.Application/Authors/Services/AuthorQueryService.cs`
- `BookStore.Infrastructure/Repository/AuthorRepository.cs` (partial — read methods)
- `BookStore.API/Controllers/AuthorsController.cs` (partial — GET endpoints)
- `BookStore.Application.Tests/Application/Authors/AuthorQueryServiceTests.cs`

### Files to modify
- `BookStore.Infrastructure/DI/InfrastructureServiceExtensions.cs` — register `IAuthorRepository`
- `BookStore.API/DI/ApplicationServiceExtensions.cs` — register `IAuthorQueryService`

### Acceptance Criteria
- [ ] `AuthorDto` — `Id, FullName, Bio, AvatarUrl (presigned), BookCount, CreatedAt, UpdatedAt`
- [ ] `AuthorDetailDto` — thêm `List<AuthorBookDto> Books`
- [ ] `AuthorBookDto` — `Id, Title, ISBN`
- [ ] `GetAuthorsQuery : QueryParams` — kế thừa `SearchTerm, SortBy, IsAscending, Page, PageSize`
- [ ] `AuthorQueryService.GetPagedAsync` — filter `SearchTerm` trên `FullName`, sort, paging qua `ApplySort().ToPagedResultAsync()`
- [ ] `AuthorQueryService.GetByIdAsync` — include `BookAuthors.Book`, generate presigned URL từ `AvatarUrl` (ObjectKey)
- [ ] `AvatarUrl` trong DTO là presigned URL — `null` nếu author chưa có avatar
- [ ] `AuthorRepository.GetQueryable()` — trả `IQueryable<Author>` (no tracking)
- [ ] `AuthorRepository.GetByIdWithBooksAsync` — include `BookAuthors.Book`
- [ ] `GET /api/authors` — anonymous, trả `200 PagedResult<AuthorDto>`
- [ ] `GET /api/authors/{id}` — anonymous, trả `200 AuthorDetailDto` hoặc `404`

### Unit Tests
```
GetByIdAsync_ShouldReturnAuthorDetail_WhenFound
GetByIdAsync_ShouldReturnNotFound_WhenMissing
GetPagedAsync_ShouldApplySearchTerm
GetPagedAsync_ShouldReturnEmptyWhenNoMatch
GetPagedAsync_ShouldGeneratePresignedUrlForEachAuthor
```

### Verification
- [ ] `dotnet build` — no error/warning
- [ ] `dotnet test` — tất cả test pass
- [ ] Swagger: `GET /api/authors` trả `ApiResponse<PagedResult<AuthorDto>>`
- [ ] `AvatarUrl` là presigned URL (không phải ObjectKey raw)

---

## Checkpoint 2: Read APIs complete

- [ ] `dotnet build` sạch
- [ ] `dotnet test` pass
- [ ] Swagger GET endpoints hoạt động đúng format
- [ ] Không có N+1 query (BookAuthors được Include một lần)

---

## Slice 3: Write — POST + PUT (Create + Update)

> **Objective:** Admin có thể tạo và cập nhật tác giả. Validate `FullName` unique ở Application layer, validate format ở API layer.

### Files to create
- `BookStore.Application/Authors/Commands/CreateAuthorCommand.cs`
- `BookStore.Application/Authors/Commands/UpdateAuthorCommand.cs`
- `BookStore.Application/Authors/IService/IAuthorCommandService.cs` (partial — Create, Update)
- `BookStore.Application/Authors/Services/AuthorCommandService.cs` (partial — Create, Update)
- `BookStore.API/Validators/CreateAuthorCommandValidator.cs`
- `BookStore.API/Validators/UpdateAuthorCommandValidator.cs`

### Files to modify
- `BookStore.Infrastructure/Repository/AuthorRepository.cs` — thêm `Add`, `ExistsByFullNameAsync`
- `BookStore.API/Controllers/AuthorsController.cs` — thêm POST, PUT endpoints
- `BookStore.API/DI/ApplicationServiceExtensions.cs` — register `IAuthorCommandService`
- `BookStore.Application.Tests/Application/Authors/AuthorCommandServiceTests.cs` — tạo file

### Acceptance Criteria
- [ ] `CreateAuthorCommand(string FullName, string? Bio)` — record
- [ ] `UpdateAuthorCommand(string FullName, string? Bio)` — record
- [ ] `CreateAuthorCommandValidator` — `FullName`: NotEmpty + MaxLength(150), `Bio`: MaxLength(2000), có `.WithMessage("...")` kết thúc dấu chấm
- [ ] `UpdateAuthorCommandValidator` — tương tự CreateAuthorCommandValidator
- [ ] `AuthorCommandService.CreateAsync` — check `ExistsByFullNameAsync` → `Author.FullNameExists` nếu trùng; tạo `Author.Create()`; `_repo.Add()`; `SaveChangesAsync`; trả `Result<Guid>`
- [ ] `AuthorCommandService.UpdateAsync` — check author tồn tại; check FullName unique (nếu đổi tên); gọi `author.Update()`; `SaveChangesAsync`
- [ ] `POST /api/authors` — `[Authorize(Roles = "Admin")]`, trả `201 Created` với `AuthorDto`
- [ ] `PUT /api/authors/{id}` — `[Authorize(Roles = "Admin")]`, trả `200` hoặc `404`/`409`

### Unit Tests
```
CreateAsync_ShouldReturnGuid_WhenValidCommand
CreateAsync_ShouldFail_WhenFullNameAlreadyExists     → Author.FullNameExists
UpdateAsync_ShouldFail_WhenAuthorNotFound            → Author.NotFound
UpdateAsync_ShouldFail_WhenNewFullNameAlreadyExists  → Author.FullNameExists
UpdateAsync_ShouldSucceed_WhenSameFullNameUnchanged
```

### Verification
- [ ] `dotnet build` — no error/warning
- [ ] `dotnet test` — tất cả test pass
- [ ] `POST /api/authors` — FluentValidation chặn `FullName` rỗng (400)
- [ ] `POST /api/authors` — trả `409` khi tên đã tồn tại

---

## Slice 4: Delete — DELETE với guard HasBooks

> **Objective:** Admin có thể xóa tác giả. Không thể xóa nếu author còn liên kết với Book.

### Files to modify
- `BookStore.Application/Authors/IService/IAuthorCommandService.cs` — thêm `DeleteAsync`
- `BookStore.Application/Authors/Services/AuthorCommandService.cs` — thêm `DeleteAsync`
- `BookStore.Infrastructure/Repository/AuthorRepository.cs` — thêm `HasBooksAsync`, `Remove`
- `BookStore.API/Controllers/AuthorsController.cs` — thêm DELETE endpoint
- `BookStore.Application.Tests/Application/Authors/AuthorCommandServiceTests.cs` — thêm Delete tests

### Acceptance Criteria
- [ ] `AuthorCommandService.DeleteAsync` — check tồn tại (`NotFound`), check `HasBooksAsync` → `Author.HasBooks` nếu còn liên kết, sau đó `_repo.Remove()` + `SaveChangesAsync`
- [ ] `AuthorRepository.HasBooksAsync` — check `BookAuthors` collection có ít nhất 1 entry cho author
- [ ] `DELETE /api/authors/{id}` — `[Authorize(Roles = "Admin")]`, trả `204` khi thành công
- [ ] Hard delete (không soft delete)

### Unit Tests
```
DeleteAsync_ShouldSucceed_WhenNoLinkedBooks
DeleteAsync_ShouldFail_WhenAuthorHasLinkedBooks   → Author.HasBooks
DeleteAsync_ShouldFail_WhenAuthorNotFound         → Author.NotFound
```

### Verification
- [ ] `dotnet build` — no error/warning
- [ ] `dotnet test` — tất cả test pass
- [ ] `DELETE` trả `409` khi author còn liên kết Book

---

## Checkpoint 3: CRUD complete

- [ ] `dotnet build` sạch
- [ ] `dotnet test` pass (tất cả Author tests)
- [ ] Swagger CRUD endpoints hoạt động đúng
- [ ] Result Pattern: không có `throw` cho lỗi nghiệp vụ trong Author services

---

## Slice 5: Avatar — PATCH `/api/authors/{id}/avatar`

> **Objective:** Admin có thể upload avatar cho tác giả. Avatar đi qua `IMediaService`, lưu ObjectKey, tự động xóa ảnh cũ.

### Files to modify
- `BookStore.Application/Authors/IService/IAuthorCommandService.cs` — thêm `UploadAvatarAsync`
- `BookStore.Application/Authors/Services/AuthorCommandService.cs` — thêm `UploadAvatarAsync`
- `BookStore.API/Controllers/AuthorsController.cs` — thêm PATCH endpoint
- `BookStore.Application.Tests/Application/Authors/AuthorCommandServiceTests.cs` — thêm Avatar tests

### Acceptance Criteria
- [ ] `AuthorCommandService.UploadAvatarAsync`:
  1. Load author → `NotFound` nếu không có
  2. Validate `file.Length <= 5_242_880` (5 MB) → `Author.AvatarTooLarge`
  3. Validate extension (`.jpg`, `.jpeg`, `.png`, `.webp`) → `Author.AvatarInvalidFormat`
  4. Nếu `author.AvatarUrl != null` (là MediaId hay ObjectKey) → gọi `_mediaService.DeleteAsync(...)` để xóa ảnh cũ
  5. Gọi `_mediaService.UploadAsync(new UploadMediaCommand { File = file, Module = "authors", UploadedBy = uploadedBy })`
  6. Gọi `author.SetAvatar(mediaResult.Value.ObjectKey)` — lưu ObjectKey, không lưu presigned URL
  7. `SaveChangesAsync` → trả presigned URL từ `mediaResult.Value.Url`
- [ ] `PATCH /api/authors/{id}/avatar` — `[Authorize(Roles = "Admin")]`, `[FromForm] IFormFile file`
- [ ] Bucket trong MinIO: `"authors"` (key trong `MinioSettings.Buckets`) → `"author-avatars"` (đã có trong `appsettings.json`)
- [ ] Khi GET author sau upload → `AvatarUrl` là presigned URL mới (QueryService generate on-the-fly)

### Unit Tests
```
UploadAvatarAsync_ShouldFail_WhenAuthorNotFound           → Author.NotFound
UploadAvatarAsync_ShouldFail_WhenFileTooLarge             → Author.AvatarTooLarge
UploadAvatarAsync_ShouldFail_WhenInvalidFormat            → Author.AvatarInvalidFormat
UploadAvatarAsync_ShouldDeleteOldAvatar_WhenAvatarExists
UploadAvatarAsync_ShouldReturnPresignedUrl_WhenSuccess
UploadAvatarAsync_ShouldNotDeleteOldAvatar_WhenNoExistingAvatar
```

### Verification
- [ ] `dotnet build` — no error/warning
- [ ] `dotnet test` — tất cả test pass
- [ ] `PATCH /api/authors/{id}/avatar` — upload file `.jpg` trả `200 { avatarUrl: "https://..." }`
- [ ] Upload `.gif` → trả `400 Author.AvatarInvalidFormat`
- [ ] Upload > 5MB → trả `400 Author.AvatarTooLarge`
- [ ] GET author sau upload → `AvatarUrl` là presigned URL (không phải ObjectKey raw)

---

## Checkpoint 4: Module complete

- [ ] `dotnet build` sạch
- [ ] `dotnet test` pass — tất cả Author tests (Command + Query)
- [ ] SOLID checklist:
  - [ ] SRP: `AuthorQueryService` và `AuthorCommandService` tách biệt, không log/send email
  - [ ] OCP: không có `switch-on-type` trong domain logic
  - [ ] LSP: không có override ném `NotSupportedException`
  - [ ] ISP: `IAuthorQueryService` và `IAuthorCommandService` tách biệt
  - [ ] DIP: không `new` dependency, inject qua constructor
- [ ] Dependency rule: Application không import Infrastructure; Domain không import bất cứ gì ngoài
- [ ] Không có `throw` cho lỗi nghiệp vụ trong toàn bộ Authors module
- [ ] `AvatarUrl` lưu ObjectKey (không phải presigned URL) trong DB
- [ ] Presigned URL chỉ generate khi trả về DTO

---

## Mocks cần cho Tests

```csharp
Mock<IAuthorRepository>    _mockAuthorRepo;
Mock<IMediaService>        _mockMediaService;
Mock<IMinioStorageService> _mockMinioService;
Mock<IUnitOfWork>          _mockUnitOfWork;
Mock<IOptions<MinioSettings>> _mockMinioSettings;
```

---

## Ghi chú quan trọng từ codebase

| Điểm | Chi tiết |
|------|----------|
| Bucket `authors` | Đã có trong `appsettings.json` → `"author-avatars"` — **không cần thêm** |
| `AuthorErrors.cs` | Đặt tại `BookStore.Domain/Errors/` (cùng với `CategoryErrors.cs`) |
| `GetQueryable()` | Không dùng `AsNoTracking()` tại đây — QueryService tự quyết định |
| MinIO bucket | Inject `IOptions<MinioSettings>` trong `AuthorQueryService` để lấy tên bucket |
| `HandleResult` | `BaseController` đã có `HandleResult`, `HandlePagedResult`, `HandleCreated` |
| `BookConfiguration` | Xóa dòng `.HasMany(b => b.Authors).WithMany(a => a.Books).UsingEntity(...)` |
