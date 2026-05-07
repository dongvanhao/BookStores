# TODO: Media Module — MinIO Integration

> **Spec:** `docs/specs/media-module.md`
> **Branch:** `feature/add-media-module`
> **Strategy:** Vertical slices — mỗi slice build được và test được độc lập

---

## Slice 0: Prerequisites — Foundation (Không phải feature slice, nhưng bắt buộc trước)

> **Objective:** Sửa những thứ bị thiếu/sai trong codebase hiện tại để các slice sau có thể hoạt động.

### Task 0.1: Map `ErrorType.Forbidden` → HTTP 403

**Vấn đề:** `ErrorType.Forbidden = 5` đã có trong enum nhưng `ResultExtensions.ToActionResult()` không xử lý → rơi vào default case → 500 InternalServerError.

**Files to modify:**
- `src/BE/Core/BookStore.Shared/Extensions/ResultExtensions.cs` — thêm case `Forbidden` → `StatusCode(403, ...)`

**Acceptance Criteria:**
- [ ] `ResultExtensions` map `ErrorType.Forbidden` → HTTP 403 `ForbiddenObjectResult`
- [ ] Build pass

---

### Task 0.2: Setup MinIO Infrastructure

**Vấn đề:** `.env` có config MinIO nhưng không có DI registration, không có service class. Chỉ có `MinioHealthCheck`.

**Files to create/modify:**
- `src/BE/Core/BookStore.Infrastructure/Storage/IMinioStorageService.cs` — interface
- `src/BE/Core/BookStore.Infrastructure/Storage/MinioStorageService.cs` — wrap MinIO SDK
- `src/BE/Core/BookStore.Infrastructure/Settings/MinioSettings.cs` — POCO config (Buckets dict, PresignedUrlExpirySeconds, AllowedMimeTypes, MaxFileSizeBytes)
- `src/BE/Core/BookStore.API/Extensions/ServiceExtensions.cs` — đăng ký `IMinioClient`, `IMinioStorageService`, `MinioSettings`, bucket initialization

**`IMinioStorageService` contract:**
```csharp
public interface IMinioStorageService
{
    Task UploadAsync(string bucketName, string objectKey, Stream stream, string contentType, long size, CancellationToken ct);
    Task DeleteAsync(string bucketName, string objectKey, CancellationToken ct);
    Task<string> GeneratePresignedUrlAsync(string bucketName, string objectKey, int expirySeconds);
    Task EnsureBucketsAsync(IEnumerable<string> bucketNames, CancellationToken ct);
}
```

**`appsettings.json` section:**
```json
"MinioSettings": {
  "Endpoint": "localhost:9000",
  "AccessKey": "minioadmin",
  "SecretKey": "minioadmin",
  "UseSsl": false,
  "Buckets": {
    "Books":   "books",
    "Authors": "authors",
    "Users":   "users"
  },
  "PresignedUrlExpirySeconds": 3600,
  "AllowedMimeTypes": ["image/jpeg","image/png","image/webp","image/gif","application/pdf"],
  "MaxFileSizeBytes": 10485760
}
```

**Acceptance Criteria:**
- [ ] `IMinioClient` được inject đúng từ `MinioSettings`
- [ ] `MinioStorageService` wrap đủ 4 methods
- [ ] Buckets được tạo tự động khi startup nếu chưa tồn tại, set **private**
- [ ] `MinioHealthCheck` dùng DI đúng (không còn resolve manual)
- [ ] `dotnet build` pass

---

### Checkpoint 0: Prerequisites Complete

- [ ] `dotnet build` sạch, không warning
- [ ] Forbidden → 403 đã map
- [ ] MinIO service có thể inject được

---

## Slice 1: Upload Media (F1)

> **Objective:** User đã đăng nhập có thể upload một file → nhận về `MediaDto` với presigned URL.
> **HTTP:** `POST /api/media/upload` → 201

### Task 1.1: Domain — `Media` Entity + `MediaType` Enum

**Files to create:**
- `src/BE/Core/BookStore.Domain/Entities/Media.cs`
- `src/BE/Core/BookStore.Domain/Enums/MediaType.cs`

**Factory method signature:**
```csharp
public static Media Create(
    string objectKey, string? thumbnailKey, string bucketName, string module,
    string originalFileName, string mimeType, long size,
    int? width, int? height, MediaType type, Guid uploadedBy)
```

**Domain method:**
```csharp
public Result CanDelete(Guid requestingUserId, bool isAdmin)
```

**Invariants (enforce trong factory, trả `Result` nếu vi phạm — hoặc throw cho programmer error):**
- `size > 0`
- `objectKey`, `bucketName`, `module`, `mimeType`, `originalFileName` không được rỗng

**Acceptance Criteria:**
- [ ] `Media.Create(...)` trả entity hợp lệ với `Id` mới
- [ ] `CanDelete` trả `Result.Success()` nếu owner hoặc admin
- [ ] `CanDelete` trả `MediaErrors.Forbidden` nếu không phải owner/admin

---

### Task 1.2: Application — `MediaErrors` + `IMediaService` (Upload)

**Files to create:**
- `src/BE/Core/BookStore.Application/Media/MediaErrors.cs`
- `src/BE/Core/BookStore.Application/Media/IService/IMediaService.cs`
- `src/BE/Core/BookStore.Application/Media/Commands/UploadMediaCommand.cs`
- `src/BE/Core/BookStore.Application/Media/DTOs/MediaDto.cs`

**`UploadMediaCommand`:**
```csharp
public sealed record UploadMediaCommand
{
    public IFormFile File          { get; init; }
    public string    Module        { get; init; }  // "books" | "authors" | "users"
    public int?      Width         { get; init; }
    public int?      Height        { get; init; }
    public IFormFile? ThumbnailFile { get; init; }
    public Guid      UploadedBy   { get; init; }
}
```

**`IMediaService` — chỉ Upload ở slice này:**
```csharp
public interface IMediaService
{
    Task<Result<MediaDto>> UploadAsync(UploadMediaCommand cmd, CancellationToken ct = default);
}
```

**Acceptance Criteria:**
- [ ] `MediaErrors` có: `NotFound`, `Forbidden`, `InvalidFileType`, `FileTooLarge`, `UploadFailed`, `DeleteFailed`
- [ ] `MediaDto` record có đủ fields (Id, Url, ThumbnailUrl, ObjectKey, Type, MimeType, Size, Width, Height, CreatedAt, CreatedBy)
- [ ] `IMediaService` ở đúng namespace `BookStore.Application.Media.IService`

---

### Task 1.3: Infrastructure — `MediaRepository` + EF Config + Migration

**Files to create:**
- `src/BE/Core/BookStore.Domain/IRepository/IMediaRepository.cs`
- `src/BE/Core/BookStore.Infrastructure/Repository/MediaRepository.cs`
- `src/BE/Core/BookStore.Infrastructure/Data/Configurations/MediaConfiguration.cs`
- Migration: `AddMediaEntity`

**`IMediaRepository`:**
```csharp
public interface IMediaRepository
{
    Task<Media?> GetByIdAsync(Guid id, CancellationToken ct = default);
    void Add(Media media);
    void Remove(Media media);
}
```

**`MediaConfiguration` key points:**
```csharp
builder.HasKey(m => m.Id);
builder.Property(m => m.ObjectKey).IsRequired().HasMaxLength(500);
builder.HasIndex(m => m.ObjectKey).IsUnique();
builder.Property(m => m.BucketName).IsRequired().HasMaxLength(100);
builder.Property(m => m.Module).IsRequired().HasMaxLength(50);
builder.Property(m => m.MimeType).IsRequired().HasMaxLength(100);
builder.Property(m => m.OriginalFileName).IsRequired().HasMaxLength(255);
builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(m => m.UploadedBy).OnDelete(DeleteBehavior.Restrict);
builder.HasIndex(m => new { m.UploadedBy, m.Module, m.CreatedAt });
```

**Acceptance Criteria:**
- [ ] `AppDbContext` có `DbSet<Media> Media`
- [ ] Migration `AddMediaEntity` chạy được
- [ ] `MediaRepository` implement đủ `IMediaRepository`

---

### Task 1.4: Application — `MediaService.UploadAsync` Implementation

**Files to create:**
- `src/BE/Core/BookStore.Application/Media/Services/MediaService.cs`

**Upload logic:**
1. Validate MIME type (so sánh với `MinioSettings.AllowedMimeTypes`) → trả `MediaErrors.InvalidFileType`
2. Validate file size → trả `MediaErrors.FileTooLarge`
3. Generate `objectKey`: `{module}/{yyyy}/{MM}/{dd}/{newGuid}.{ext}`
4. Nếu `ThumbnailFile != null`: generate `thumbnailKey`: `{module}/thumbnails/{yyyy}/{MM}/{dd}/{newGuid}.{ext}`
5. Upload main file qua `IMinioStorageService`
6. Upload thumbnail file (nếu có) qua `IMinioStorageService`
7. `Media.Create(...)` → `_mediaRepo.Add(media)` → `_unitOfWork.SaveChangesAsync()`
8. Generate presigned URL qua `IMinioStorageService`
9. Map entity → `MediaDto` → trả `Result<MediaDto>`

**Constructor inject:** `IMediaRepository`, `IUnitOfWork`, `IMinioStorageService`, `IOptions<MinioSettings>`

**Acceptance Criteria:**
- [ ] Upload thành công: `Media` record trong DB, file trong MinIO, trả `MediaDto` với URL
- [ ] Upload fail MIME type → `MediaErrors.InvalidFileType` (không lưu DB, không upload)
- [ ] Upload fail size → `MediaErrors.FileTooLarge`
- [ ] MinIO exception → trả `MediaErrors.UploadFailed` (không rethrow)

---

### Task 1.5: API — `MediaController` (Upload) + Validator + DI

**Files to create/modify:**
- `src/BE/Core/BookStore.API/Controllers/MediaController.cs`
- `src/BE/Core/BookStore.API/Validators/UploadMediaRequestValidator.cs`
- `src/BE/Core/BookStore.API/Extensions/ServiceExtensions.cs` — đăng ký `IMediaService`, `IMediaRepository`

**Endpoint:**
```
POST /api/media/upload
Content-Type: multipart/form-data
[Authorize]
→ 201 + ApiResponse<MediaDto>
```

**Validator rules:**
- `file` required
- `module` required, phải thuộc `["books", "authors", "users"]`
- MIME type của `file` trong `AllowedMimeTypes` (inject `IOptions<MinioSettings>`)
- File size ≤ `MaxFileSizeBytes`
- `width` ≥ 0 nếu có, `height` ≥ 0 nếu có

**Acceptance Criteria:**
- [ ] `POST /api/media/upload` → 201 với `MediaDto`
- [ ] Invalid MIME type → 400 từ validator (trước khi vào service)
- [ ] Unauthenticated → 401
- [ ] XML doc comments + `[ProducesResponseType]` đầy đủ

---

### Task 1.6: Unit Tests — Slice 1

**Files to create:**
- `tests/BookStore.UnitTests/Domain/Media/MediaTests.cs`
- `tests/BookStore.UnitTests/Application/Media/MediaServiceUploadTests.cs`

**Domain tests:**
- `Create_ShouldReturnMedia_WithCorrectProperties`
- `CanDelete_ShouldReturnSuccess_WhenOwner`
- `CanDelete_ShouldReturnSuccess_WhenAdmin`
- `CanDelete_ShouldReturnForbidden_WhenNotOwnerNotAdmin`

**Service tests:**
- `UploadAsync_ShouldReturnMediaDto_WhenSuccess`
- `UploadAsync_ShouldFail_WhenInvalidMimeType`
- `UploadAsync_ShouldFail_WhenFileTooLarge`
- `UploadAsync_ShouldFail_WhenMinioThrows` → `MediaErrors.UploadFailed`

**Mocks:** `IMediaRepository`, `IMinioStorageService`, `IUnitOfWork`, `IOptions<MinioSettings>`

---

### Checkpoint 1: Slice 1 Complete

- [ ] `dotnet build` sạch
- [ ] `dotnet test` pass
- [ ] Swagger: `POST /api/media/upload` visible, response `ApiResponse<MediaDto>` đúng
- [ ] MinIO bucket có file sau upload
- [ ] DB có `Media` record

---

## Slice 2: Delete Media (F2)

> **Objective:** Owner hoặc Admin xóa media → MinIO object biến mất, DB record bị xóa.
> **HTTP:** `DELETE /api/media/{id}` → 204 (owner/admin) | 403 (người khác)

### Task 2.1: Application — `IMediaService.DeleteAsync` + Implementation

**Files to modify:**
- `src/BE/Core/BookStore.Application/Media/IService/IMediaService.cs` — thêm `DeleteAsync`
- `src/BE/Core/BookStore.Application/Media/Services/MediaService.cs` — implement `DeleteAsync`
- `src/BE/Core/BookStore.Application/Media/Commands/DeleteMediaCommand.cs`

**`DeleteAsync` logic:**
1. Load `media` từ DB → trả `MediaErrors.NotFound` nếu không có
2. `media.CanDelete(requestingUserId, isAdmin)` → trả error nếu fail
3. `IMinioStorageService.DeleteAsync(media.BucketName, media.ObjectKey)` — nếu throw → `MediaErrors.DeleteFailed`
4. Nếu `media.ThumbnailKey != null`: xóa thumbnail
5. `_mediaRepo.Remove(media)` → `_unitOfWork.SaveChangesAsync()`

---

### Task 2.2: Infrastructure — `MediaRepository.GetByIdAsync` + `Remove`

Các methods này đã khai báo trong `IMediaRepository` từ Task 1.3 nhưng cần verify implementation đầy đủ.

---

### Task 2.3: API — `DELETE /api/media/{id}` Endpoint

**Files to modify:**
- `src/BE/Core/BookStore.API/Controllers/MediaController.cs`

**Lấy `currentUserId` từ JWT claim:** `User.FindFirstValue(ClaimTypes.NameIdentifier)`
**Lấy `isAdmin`:** `User.IsInRole("Admin")`

**Acceptance Criteria:**
- [ ] Owner delete → 204, file biến khỏi MinIO, record biến khỏi DB
- [ ] Non-owner delete → 403
- [ ] Admin delete → 204
- [ ] Not found → 404

---

### Task 2.4: Unit Tests — Slice 2

**Files to create:**
- `tests/BookStore.UnitTests/Application/Media/MediaServiceDeleteTests.cs`

- `DeleteAsync_ShouldSucceed_WhenOwner`
- `DeleteAsync_ShouldSucceed_WhenAdmin`
- `DeleteAsync_ShouldFail_WhenNotOwnerNotAdmin` → `ErrorType.Forbidden`
- `DeleteAsync_ShouldFail_WhenNotFound` → `ErrorType.NotFound`
- `DeleteAsync_ShouldFail_WhenMinioThrows` → `MediaErrors.DeleteFailed`
- `DeleteAsync_ShouldDeleteThumbnail_WhenThumbnailKeyExists`

---

### Checkpoint 2: Slice 2 Complete

- [ ] `dotnet build` + `dotnet test` pass
- [ ] Owner delete flow hoạt động end-to-end
- [ ] Unauthorized user nhận 403 (không phải 500)

---

## Slice 3: Get Single Media + Presigned URL (F3)

> **Objective:** Owner hoặc Admin lấy thông tin một media item kèm presigned URL mới.
> **HTTP:** `GET /api/media/{id}` → 200 + `MediaDto`

### Task 3.1: Application — `IMediaQueryService` + `GetByIdAsync`

**Files to create:**
- `src/BE/Core/BookStore.Application/Media/IService/IMediaQueryService.cs`
- `src/BE/Core/BookStore.Application/Media/Services/MediaQueryService.cs`

**`IMediaQueryService`:**
```csharp
public interface IMediaQueryService
{
    Task<Result<MediaDto>> GetByIdAsync(Guid id, Guid userId, bool isAdmin, CancellationToken ct = default);
}
```

**`GetByIdAsync` logic:**
1. Load media từ DB → `MediaErrors.NotFound`
2. Ownership check (`media.UploadedBy == userId || isAdmin`) → `MediaErrors.Forbidden`
3. Generate presigned URL on-the-fly
4. Map → `MediaDto`

---

### Task 3.2: API — `GET /api/media/{id}` Endpoint

**Files to modify:**
- `src/BE/Core/BookStore.API/Controllers/MediaController.cs`
- `src/BE/Core/BookStore.API/Extensions/ServiceExtensions.cs` — đăng ký `IMediaQueryService`

---

### Task 3.3: Unit Tests — Slice 3

**Files to create:**
- `tests/BookStore.UnitTests/Application/Media/MediaQueryServiceTests.cs`

- `GetByIdAsync_ShouldReturnMediaDto_WhenOwner`
- `GetByIdAsync_ShouldReturnMediaDto_WhenAdmin`
- `GetByIdAsync_ShouldFail_WhenNotOwnerNotAdmin` → 403
- `GetByIdAsync_ShouldFail_WhenNotFound` → 404

---

### Checkpoint 3: Slice 3 Complete

- [ ] `dotnet build` + `dotnet test` pass
- [ ] `GET /api/media/{id}` trả đúng `MediaDto` với presigned URL

---

## Slice 4: List Media — Cursor Pagination + Bulk Presigned URLs (F4)

> **Objective:** User xem danh sách media của mình với cursor pagination, mỗi item kèm presigned URL.
> **HTTP:** `GET /api/media?module=books&type=image&before=2026-04-19T10:00:00Z&limit=20` → 200

### Task 4.1: Application — `GetMediaListQuery` + `MediaListResponse` + `MediaCursorMeta`

**Files to create:**
- `src/BE/Core/BookStore.Application/Media/Queries/GetMediaListQuery.cs`
- `src/BE/Core/BookStore.Application/Media/DTOs/MediaListResponse.cs`
- `src/BE/Core/BookStore.Application/Media/DTOs/MediaCursorMeta.cs`

**`GetMediaListQuery`:**
```csharp
public sealed class GetMediaListQuery
{
    public string?    Module  { get; init; }
    public MediaType? Type    { get; init; }
    public DateTime?  Before  { get; init; }   // cursor
    public int        Limit   { get; init; } = 20;  // max 50
}
```

**`MediaListResponse`:**
```csharp
public sealed record MediaListResponse
{
    public IReadOnlyList<MediaDto> Data { get; init; }
    public MediaCursorMeta         Meta { get; init; }
}
```

---

### Task 4.2: Infrastructure — Cursor-Based Repository Query

**Files to modify:**
- `src/BE/Core/BookStore.Domain/IRepository/IMediaRepository.cs` — thêm `GetListAsync`
- `src/BE/Core/BookStore.Infrastructure/Repository/MediaRepository.cs` — implement cursor query

**Repository method:**
```csharp
Task<List<Media>> GetListAsync(
    Guid userId, bool isAdmin,
    string? module, MediaType? type,
    DateTime? before, int limit,
    CancellationToken ct = default);
```

**Query logic:**
```csharp
var query = _context.Media.AsNoTracking();

if (!isAdmin) query = query.Where(m => m.UploadedBy == userId);
if (module != null) query = query.Where(m => m.Module == module);
if (type != null) query = query.Where(m => m.Type == type);
if (before != null) query = query.Where(m => m.CreatedAt < before);

return await query
    .OrderByDescending(m => m.CreatedAt)
    .Take(limit + 1)  // take 1 extra để biết HasMore
    .ToListAsync(ct);
```

---

### Task 4.3: Application — `IMediaQueryService.GetListAsync` + Bulk Presigned URL

**Files to modify:**
- `src/BE/Core/BookStore.Application/Media/IService/IMediaQueryService.cs` — thêm `GetListAsync`
- `src/BE/Core/BookStore.Application/Media/Services/MediaQueryService.cs` — implement
- `src/BE/Core/BookStore.Application/Media/IService/IMediaService.cs` — thêm `GetBulkPresignedUrlsAsync`
- `src/BE/Core/BookStore.Application/Media/Services/MediaService.cs` — implement bulk

**`GetListAsync` logic:**
1. Lấy `limit + 1` items từ repo
2. `hasMore = items.Count > limit` → cắt bớt item cuối nếu `hasMore`
3. `nextCursor = hasMore ? items.Last().CreatedAt : null`
4. Gom tất cả `(bucket, key)` pairs → `GetBulkPresignedUrlsAsync`
5. Map items → `MediaDto` (inject presigned URL từ dict)
6. Trả `MediaListResponse { Data, Meta }`

**⚠️ Custom Response Format:**
Response của `GET /api/media` không phải `ApiResponse<T>` chuẩn (meta nằm ngoài data).
Dùng `ApiResponse<MediaListResponse>` — `data` sẽ chứa `{ items: [...], meta: {...} }`.
Điều chỉnh serialization tại Controller nếu FE yêu cầu flat format.

---

### Task 4.4: API — `GET /api/media` Endpoint

**Files to modify:**
- `src/BE/Core/BookStore.API/Controllers/MediaController.cs`

**Query params binding:** `[FromQuery] GetMediaListQuery query`

---

### Task 4.5: Unit Tests — Slice 4

**Files to modify:**
- `tests/BookStore.UnitTests/Application/Media/MediaQueryServiceTests.cs`

- `GetListAsync_ShouldReturnPagedResult_WithCursor`
- `GetListAsync_ShouldSetHasMoreFalse_WhenLastPage`
- `GetListAsync_ShouldFilterByModule`
- `GetListAsync_ShouldFilterByType`
- `GetListAsync_ShouldEnforceLimitMax50`

---

### Checkpoint 4: Slice 4 Complete

- [ ] `dotnet build` + `dotnet test` pass
- [ ] `GET /api/media` trả đúng schema `{ data: [...], meta: { nextCursor, limit, hasMore } }`
- [ ] Cursor pagination hoạt động: `before=...` trả trang kế tiếp

---

## Slice 5: Internal Service API — Integration với Modules Khác (F5)

> **Objective:** `IMediaService` (UploadAsync, DeleteAsync, GetPresignedUrlAsync, GetBulkPresignedUrlsAsync) sẵn sàng để Books, Authors, Users inject.

### Task 5.1: Hoàn thiện `IMediaService` Contract

**Files to modify:**
- `src/BE/Core/BookStore.Application/Media/IService/IMediaService.cs`

**Thêm:**
```csharp
Task<Result<string>> GetPresignedUrlAsync(
    string bucketName, string objectKey,
    int expirySeconds = 3600, CancellationToken ct = default);

Task<Dictionary<string, string>> GetBulkPresignedUrlsAsync(
    IEnumerable<(string bucket, string key)> items,
    int expirySeconds = 3600, CancellationToken ct = default);
```

**Ghi chú:** Methods này đã được implement rải rác trong các slice trước. Task này chỉ verify interface đầy đủ và consistent.

---

### Task 5.2: Verify Integration Point — `Book.CoverUrl`

**Ghi chú:** `Book` entity đã có `CoverUrl` property + `SetCover()` method.
`BookCommandService` (nếu/khi implement Books module) sẽ inject `IMediaService` để upload cover.

**Acceptance Criteria:**
- [ ] `IMediaService` đăng ký đúng scope (`Scoped`) — không phải Singleton (stream disposal)
- [ ] Không có circular DI dependency
- [ ] `dotnet build` pass

---

### Checkpoint FINAL: Media Module Complete

- [ ] `dotnet build` sạch
- [ ] `dotnet test` pass (tất cả tests từ Slice 1–4)
- [ ] 4 endpoints hoạt động end-to-end:
  - `POST /api/media/upload` → 201
  - `DELETE /api/media/{id}` → 204 / 403 / 404
  - `GET /api/media/{id}` → 200 / 403 / 404
  - `GET /api/media` → 200 với cursor pagination
- [ ] `ErrorType.Forbidden` → 403 verified
- [ ] MinIO buckets private, presigned URL expire sau 1h
- [ ] Ownership check qua `media.CanDelete()` — không bypass
- [ ] `objectKey` lưu DB, **không** lưu presigned URL

---

## Summary — Dependency Order

```
Slice 0 (Prerequisites)
  └─ Slice 1 (Upload) ← cần MinIO infra + Forbidden map
       └─ Slice 2 (Delete) ← cần Media entity + IMediaService
            └─ Slice 3 (Get Single) ← cần IMediaQueryService
                 └─ Slice 4 (List) ← cần cursor query + bulk presign
                      └─ Slice 5 (Internal API) ← verify contract đầy đủ
```

## Open Questions — Cần Xác Nhận Trước Khi Build

| # | Câu hỏi | Ảnh hưởng |
|---|---------|-----------|
| 1 | `GET /api/media` response: flat `{ data: [], meta: {} }` hay `{ data: { items: [], meta: {} } }` lồng nhau? | Slice 4 API layer |
| 2 | Admin có cần `GET /api/admin/media?userId=...` không? | Slice 4 hoặc Dashboard module |
| 3 | Presigned URL expiry 1h có đủ không? FE cache-control ra sao? | Task 0.2 config |
