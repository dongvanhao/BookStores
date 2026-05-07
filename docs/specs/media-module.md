# Feature: Media Module — MinIO Integration

## Objective

Xây dựng một module Media tập trung để quản lý file upload/download thông qua MinIO (private buckets + presigned URLs). Module này đóng vai trò **infrastructure service** cho toàn bộ hệ thống: Books (cover image), Authors (avatar), Users (avatar), và có thể mở rộng về sau. Ownership check bắt buộc trước khi delete.

## Module

**Media** — Cross-cutting module, phục vụ cho Books / Authors / Auth và các module khác.

---

## Core Features

### F1 — Upload Media
- Upload single file (multipart/form-data)
- Server tự generate `objectKey` theo format: `{module}/{yyyy}/{MM}/{dd}/{newGuid}.{ext}`
- Lưu metadata vào DB (`Media` entity)
- Trả về `MediaDto` có presigned URL (thời hạn 1 giờ)
- **Acceptance:** Upload thành công → HTTP 201, `Media` record trong DB, file trong MinIO bucket tương ứng

### F2 — Delete Media (Ownership Check)
- Chỉ owner (`UploadedBy == currentUserId`) hoặc Admin mới được xóa
- Xóa object khỏi MinIO + xóa record DB (hard delete)
- Nếu có thumbnail → xóa cả thumbnail key
- **Acceptance:** User khác delete → HTTP 403. Owner delete → HTTP 204, file biến mất khỏi MinIO

### F3 — Get Presigned URL (single)
- Tạo presigned URL cho một media item theo `id`
- Chỉ owner hoặc Admin (media là private)
- **Acceptance:** Presigned URL hợp lệ trong 1 giờ

### F4 — List Media (cursor-based, bulk presigned URL)
- Query media của **current user** (hoặc Admin query bất kỳ)
- Sort: `createdAt DESC` (mới nhất → cũ nhất)
- Cursor pagination (dùng `createdAt` làm cursor, không dùng offset)
- Generate presigned URL cho TẤT CẢ items trong batch (bao gồm thumbnailUrl nếu có)
- Filter tuỳ chọn: `module`, `type` (image/document/video), `limit`
- **Acceptance:** Response đúng schema với `meta.nextCursor`, `meta.hasMore`

### F5 — Internal Service API (dùng cho các module khác)
- `IMediaService` cho phép Books, Authors, Users inject và dùng upload/delete/presign
- Không expose qua HTTP — là service contract nội bộ giữa Application layers

---

## Out of Scope

- Thumbnail generation tự động (resize) — `thumbnailUrl` được lưu sẵn nếu caller tự upload thumbnail; nếu không có thì `null`
- Video transcoding
- CDN integration
- Public bucket / public URL (tất cả bucket là **private**)
- Pagination kiểu offset (`page`/`pageSize`) — dùng cursor thay thế
- Virus scanning / content moderation

---

## Technical Approach

### Domain

**Entity mới: `Media`**

```csharp
// Domain/Entities/Media.cs
public class Media : BaseEntity
{
    public string       ObjectKey        { get; private set; }   // "books/2026/04/18/{guid}.jpg"
    public string?      ThumbnailKey     { get; private set; }   // null nếu không có thumb
    public string       BucketName       { get; private set; }   // "books", "authors", "users"
    public string       Module           { get; private set; }   // "books", "authors", "users"
    public string       OriginalFileName { get; private set; }
    public string       MimeType         { get; private set; }
    public long         Size             { get; private set; }   // bytes
    public int?         Width            { get; private set; }   // pixels (null nếu không phải ảnh)
    public int?         Height           { get; private set; }
    public MediaType    Type             { get; private set; }   // enum
    public Guid         UploadedBy       { get; private set; }   // FK → ApplicationUser

    // Factory method — private ctor
    public static Media Create(
        string objectKey, string? thumbnailKey, string bucketName, string module,
        string originalFileName, string mimeType, long size,
        int? width, int? height, MediaType type, Guid uploadedBy) { ... }

    // Domain methods
    public Result CanDelete(Guid requestingUserId, bool isAdmin)
    {
        if (isAdmin || UploadedBy == requestingUserId) return Result.Success();
        return MediaErrors.Forbidden;
    }
}
```

**Enum mới: `MediaType`**

```csharp
public enum MediaType { Image, Document, Video, Other }
```

**Business Invariants:**
- `ObjectKey` không được trùng (unique index)
- `Size` > 0
- `MimeType` phải thuộc whitelist được cấu hình
- Xóa phải qua `CanDelete()` — không bypass

---

### Application

**Module structure:**

```
Application/
  Media/
    IService/
      IMediaService.cs          ← contract nội bộ cho các module khác dùng
      IMediaQueryService.cs     ← contract cho HTTP query endpoint
    Services/
      MediaService.cs
      MediaQueryService.cs
    Commands/
      UploadMediaCommand.cs
      DeleteMediaCommand.cs
    Queries/
      GetMediaListQuery.cs      ← cursor-based
    DTOs/
      MediaDto.cs
      MediaListResponse.cs      ← data[] + meta{}
      MediaCursorMeta.cs
      UploadMediaRequest.cs
    MediaErrors.cs
```

**`IMediaService` — internal contract:**

```csharp
public interface IMediaService
{
    /// <summary>Upload file lên MinIO, lưu metadata vào DB, trả về MediaDto với presigned URL.</summary>
    Task<Result<MediaDto>> UploadAsync(UploadMediaCommand cmd, CancellationToken ct = default);

    /// <summary>Xóa media — kiểm tra ownership, xóa MinIO object + DB record.</summary>
    Task<Result> DeleteAsync(Guid mediaId, Guid requestingUserId, bool isAdmin, CancellationToken ct = default);

    /// <summary>Tạo presigned URL cho một media item (không lưu DB).</summary>
    Task<Result<string>> GetPresignedUrlAsync(string bucketName, string objectKey, int expirySeconds = 3600, CancellationToken ct = default);

    /// <summary>Tạo bulk presigned URLs — dùng khi render list.</summary>
    Task<Dictionary<string, string>> GetBulkPresignedUrlsAsync(IEnumerable<(string bucket, string key)> items, int expirySeconds = 3600, CancellationToken ct = default);
}
```

**`IMediaQueryService` — HTTP query:**

```csharp
public interface IMediaQueryService
{
    Task<Result<MediaListResponse>> GetListAsync(GetMediaListQuery query, Guid userId, CancellationToken ct = default);
    Task<Result<MediaDto>>          GetByIdAsync(Guid id, Guid userId, bool isAdmin, CancellationToken ct = default);
}
```

**`GetMediaListQuery`:**

```csharp
public sealed class GetMediaListQuery
{
    public string? Module    { get; init; }       // filter by module ("books", "authors", ...)
    public MediaType? Type   { get; init; }       // filter by type
    public DateTime? Before  { get; init; }       // cursor: items with createdAt < Before
    public int Limit         { get; init; } = 20; // max 50
}
```

**DTOs:**

```csharp
public sealed record MediaDto
{
    public Guid     Id             { get; init; }
    public string   Url            { get; init; }           // presigned URL
    public string?  ThumbnailUrl   { get; init; }           // presigned thumbnail URL
    public string   ObjectKey      { get; init; }
    public string   Type           { get; init; }           // "image" | "document" | "video" | "other"
    public string   MimeType       { get; init; }
    public long     Size           { get; init; }
    public int?     Width          { get; init; }
    public int?     Height         { get; init; }
    public DateTime CreatedAt      { get; init; }
    public string   CreatedBy      { get; init; }           // userId string
}

public sealed record MediaListResponse
{
    public IReadOnlyList<MediaDto> Data { get; init; }
    public MediaCursorMeta         Meta { get; init; }
}

public sealed record MediaCursorMeta
{
    public DateTime? NextCursor { get; init; }   // null nếu không còn item nào
    public int       Limit      { get; init; }
    public bool      HasMore    { get; init; }
}
```

**`MediaErrors.cs`:**

```csharp
public static class MediaErrors
{
    public static Error NotFound(Guid id) => Error.NotFound("Media.NotFound", $"Media '{id}' not found.");
    public static readonly Error Forbidden       = Error.Forbidden("Media.Forbidden", "You are not allowed to delete this media.");
    public static readonly Error InvalidFileType = Error.Validation("Media.InvalidFileType", "File type is not allowed.");
    public static readonly Error FileTooLarge    = Error.Validation("Media.FileTooLarge", "File exceeds maximum allowed size.");
    public static readonly Error UploadFailed    = Error.Failure("Media.UploadFailed", "Failed to upload file to storage.");
    public static readonly Error DeleteFailed    = Error.Failure("Media.DeleteFailed", "Failed to delete file from storage.");
}
```

---

### Infrastructure

**MinIO abstraction:**

```
Infrastructure/
  Storage/
    IMinioStorageService.cs     ← interface trong Infrastructure (internal abstraction)
    MinioStorageService.cs      ← wrap MinIO SDK
  Repository/
    MediaRepository.cs
  Data/Configurations/
    MediaConfiguration.cs
```

**`MinioStorageService` — wraps MinIO SDK:**

```csharp
public interface IMinioStorageService
{
    Task UploadAsync(string bucketName, string objectKey, Stream stream, string contentType, long size, CancellationToken ct);
    Task DeleteAsync(string bucketName, string objectKey, CancellationToken ct);
    Task<string> GeneratePresignedUrlAsync(string bucketName, string objectKey, int expirySeconds);
}
```

**ObjectKey format:**
```
{module}/{yyyy}/{MM}/{dd}/{Guid}.{ext}

Ví dụ:
  books/2026/04/18/3f2a1b4c-....jpg
  authors/2026/04/18/9e8d7c6b-....png
  users/2026/04/18/1a2b3c4d-....jpg

Thumbnail (nếu có):
  books/thumbnails/2026/04/18/3f2a1b4c-....jpg
```

**Bucket mapping (cấu hình):**

```json
// appsettings.json
"MinioSettings": {
  "Endpoint":   "localhost:9000",
  "AccessKey":  "...",
  "SecretKey":  "...",
  "UseSsl":     false,
  "Buckets": {
    "Books":    "books",
    "Authors":  "authors",
    "Users":    "users"
  },
  "PresignedUrlExpirySeconds": 3600,
  "AllowedMimeTypes": [
    "image/jpeg", "image/png", "image/webp", "image/gif",
    "application/pdf"
  ],
  "MaxFileSizeBytes": 10485760
}
```

> Tất cả bucket được tạo khi startup nếu chưa tồn tại, và được set **private** (không có public policy).

**EF Core — `MediaConfiguration`:**

```csharp
builder.HasKey(m => m.Id);
builder.Property(m => m.ObjectKey).IsRequired().HasMaxLength(500);
builder.HasIndex(m => m.ObjectKey).IsUnique();
builder.Property(m => m.BucketName).IsRequired().HasMaxLength(100);
builder.Property(m => m.Module).IsRequired().HasMaxLength(50);
builder.Property(m => m.MimeType).IsRequired().HasMaxLength(100);
builder.Property(m => m.OriginalFileName).IsRequired().HasMaxLength(255);
builder.Property(m => m.Size).IsRequired();

builder.HasOne<ApplicationUser>()
       .WithMany()
       .HasForeignKey(m => m.UploadedBy)
       .OnDelete(DeleteBehavior.Restrict);

// Index cho cursor pagination (sort DESC by CreatedAt, filter by UploadedBy + Module)
builder.HasIndex(m => new { m.UploadedBy, m.Module, m.CreatedAt });
```

**Migration name:** `AddMediaEntity`

---

### API

**Controller: `MediaController` — `/api/media`**

| Method | Route | Auth | Mô tả |
|--------|-------|------|-------|
| `POST` | `/api/media/upload` | `[Authorize]` | Upload single file |
| `DELETE` | `/api/media/{id:guid}` | `[Authorize]` | Delete (ownership check) |
| `GET` | `/api/media/{id:guid}` | `[Authorize]` | Lấy 1 item + presigned URL |
| `GET` | `/api/media` | `[Authorize]` | List own media, bulk presigned URLs |

**Request — Upload:**

```http
POST /api/media/upload
Content-Type: multipart/form-data

file:   <binary>
module: "books"          ← required: "books" | "authors" | "users"
width:  1080             ← optional, caller tự set (không auto-detect)
height: 720              ← optional
thumbnailFile: <binary>  ← optional
```

**Query — List:**

```
GET /api/media?module=books&type=image&before=2026-04-19T10:00:00Z&limit=20
```

**Response — List (HTTP 200):**

```json
{
  "success": true,
  "data": [
    {
      "id": "uuid",
      "url": "https://minio:9000/books/2026/04/18/...?X-Amz-Signature=...",
      "thumbnailUrl": "https://minio:9000/books/thumbnails/2026/04/18/...?X-Amz-Signature=...",
      "objectKey": "books/2026/04/18/3f2a1b4c-xxx.jpg",
      "type": "image",
      "mimeType": "image/jpeg",
      "size": 123456,
      "width": 1080,
      "height": 720,
      "createdAt": "2026-04-19T10:00:00Z",
      "createdBy": "user-uuid"
    }
  ],
  "meta": {
    "nextCursor": "2026-04-19T09:00:00Z",
    "limit": 20,
    "hasMore": true
  }
}
```

> Lưu ý: `meta` ở ngoài `data`, không nằm trong `ApiResponse<T>.Data` — cần `ApiResponse` hỗ trợ hoặc dùng custom response wrapper.

**Upload Validator (`UploadMediaRequestValidator`):**

| Rule | Tầng | Công cụ |
|------|------|---------|
| `file` required | API | FluentValidation |
| `module` required, in whitelist | API | FluentValidation |
| MIME type trong AllowedMimeTypes | API | FluentValidation (đọc từ config) |
| File size ≤ MaxFileSizeBytes | API | FluentValidation |
| `width`/`height` ≥ 0 nếu có | API | FluentValidation |

---

## Validation Plan

| Rule | Tầng | Công cụ |
|------|------|---------|
| file required, size, mimetype | API | FluentValidation |
| module in whitelist | API | FluentValidation |
| width/height optional, ≥ 0 | API | FluentValidation |
| Ownership check khi delete | Application | `media.CanDelete()` → Result |
| Media tồn tại | Application | `MediaErrors.NotFound` |
| Bucket tồn tại (startup) | Infrastructure | `MinioStorageService.EnsureBucketsAsync()` |

---

## Testing Strategy

**Unit tests — `MediaService`:**
- `UploadAsync_ShouldReturnMediaDto_WhenSuccess` — mock `IMinioStorageService` + mock `IMediaRepository`
- `UploadAsync_ShouldFail_WhenInvalidMimeType` — validator tầng API trước, nhưng test service guard
- `DeleteAsync_ShouldSucceed_WhenOwner`
- `DeleteAsync_ShouldFail_WhenNotOwnerAndNotAdmin` → `ErrorType.Forbidden`
- `DeleteAsync_ShouldSucceed_WhenAdmin`
- `GetListAsync_ShouldReturnPagedResult_WithCursor`
- `GetListAsync_ShouldSetHasMoreFalse_WhenLastPage`

**Unit tests — `Media` domain:**
- `CanDelete_ShouldReturnSuccess_WhenOwner`
- `CanDelete_ShouldReturnSuccess_WhenAdmin`
- `CanDelete_ShouldReturnForbidden_WhenNotOwnerNotAdmin`

**Mock:** `IMinioStorageService`, `IMediaRepository`, `IUnitOfWork`

---

## Integration với các Module khác

Các module khác inject `IMediaService` để upload/delete:

```csharp
// Ví dụ trong BookCommandService
public async Task<Result<Guid>> CreateAsync(CreateBookCommand cmd, CancellationToken ct)
{
    // upload cover image → nhận về MediaDto (có presigned URL)
    var uploadCmd = new UploadMediaCommand { File = cmd.CoverFile, Module = "books", ... };
    var mediaResult = await _mediaService.UploadAsync(uploadCmd, ct);
    if (!mediaResult.IsSuccess) return mediaResult.Error;

    var book = Book.Create(cmd.Title, cmd.Price, mediaResult.Value.ObjectKey, ...);
    _bookRepo.Add(book);
    await _unitOfWork.SaveChangesAsync(ct);
    return book.Id;
}
```

> `Book.CoverUrl` (và `Author.AvatarUrl`) sẽ lưu `objectKey`, **không** lưu presigned URL (URL có expire). Presigned URL được generate on-the-fly khi cần hiển thị.

---

## Boundaries

### Always Do
- Presigned URL **không** lưu DB — generate on-the-fly mỗi request
- `objectKey` là identifier bền vững được lưu vào DB/Entity
- Bucket luôn **private** — không set public policy
- Ownership check qua domain method `CanDelete()` — không bypass
- `ErrorType.Forbidden` → HTTP 403 (cần bổ sung map trong `ResultExtensions`)

### Ask First
- Thêm bucket mới ngoài `books`, `authors`, `users`
- Tự động generate thumbnail (cần thêm `SixLabors.ImageSharp` dependency)
- Tăng `MaxFileSizeBytes` > 10MB

### Never Do
- Lưu presigned URL vào DB
- Dùng public bucket URL thay presigned URL
- Bypass `CanDelete()` trong service
- Gọi MinIO SDK trực tiếp trong Application layer — phải qua `IMinioStorageService`

---

## Dependency & Package

| Package | Đã có | Ghi chú |
|---------|-------|---------|
| `Minio` 6.0.2 | ✅ | Trong Infrastructure.csproj |
| `SixLabors.ImageSharp` | ❌ | Chỉ cần nếu auto-generate thumbnail (out of scope MVP) |

---

## Open Questions (cần xác nhận trước khi build)

1. **`ErrorType.Forbidden`** đã tồn tại trong `Shared/Results/ErrorType.cs` chưa? (Explore thấy có `Forbidden` trong enum nhưng cần xác nhận `ResultExtensions` map sang HTTP 403)
2. **Thumbnail upload:** Caller tự upload file thumbnail riêng, hay server auto-resize? (MVP = caller tự upload, server chỉ lưu `thumbnailKey`)
3. **Admin list all media:** Admin có cần endpoint `GET /api/admin/media?userId=...` để xem media của user bất kỳ không? (Có thể là phần của Dashboard module, không phải MVP của Media module)
4. **Presigned URL expiry:** 1 giờ có đủ không? FE cần cache-control ra sao?
