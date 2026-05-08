# Feature: Categories Module

## Objective
Xây dựng CRUD + hierarchical tree API cho entity `Category` — hỗ trợ đa cấp (Parent–Child) với validation chặt chẽ: không cho self-parent và không tạo vòng lặp phụ thuộc (circular reference).

## Module
`Categories`

---

## Core Features

### 1. CRUD — Acceptance Criteria
- **Create**: Tạo category mới với Name (required), Description (optional), ParentId (optional). Trả `CategoryDto` với HTTP 201.
- **Get by ID**: Lấy category theo Id, bao gồm thông tin Parent nếu có.
- **Get list (flat)**: Lấy danh sách phân trang, hỗ trợ `searchTerm`, `sortBy`, `page`, `pageSize`.
- **Update**: Sửa Name, Description, ParentId. Validate circular reference trước khi lưu.
- **Delete**: Xóa vật lý (hard delete). Từ chối nếu category còn child hoặc còn sách đang dùng.

### 2. Get Tree — Acceptance Criteria
- `GET /api/categories/tree` — Trả toàn bộ cây category dạng nested JSON (chỉ root categories, Children được nest đệ quy).
- `GET /api/categories/{id}/subtree` — Trả subtree bắt đầu từ category `id`.
- Không phân trang — tree luôn trả full.

### 3. Validation — Acceptance Criteria
- **Self-parent**: `parentId == id` → HTTP 400, `Category.SelfParent`.
- **Circular loop**: Nếu set `parentId = X` mà X là descendant của category hiện tại → HTTP 400, `Category.CircularReference`.
- **Delete with children**: Nếu category có child → HTTP 409, `Category.HasChildren`.
- **Delete with books**: Nếu category có sách → HTTP 409, `Category.HasBooks`.
- **Parent not found**: `parentId` được cung cấp nhưng không tồn tại → HTTP 404, `Category.ParentNotFound`.

### 4. Icon Upload (tích hợp Media module) — Acceptance Criteria
- Upload icon/thumbnail cho category qua `POST /api/categories/{id}/icon`
- Sử dụng `IMediaService.UploadAsync` với `module = "categories"` — không upload trực tiếp lên MinIO
- `Category` entity lưu `IconObjectKey` (string?, nullable) — không lưu presigned URL
- Khi `GET /api/categories/{id}` trả về, generate presigned URL on-the-fly từ `IconObjectKey` qua `IMinioStorageService`
- Xóa icon: `DELETE /api/categories/{id}/icon` — gọi `IMediaService.DeleteAsync`, set `IconObjectKey = null`
- **Acceptance:** Upload thành công → `CategoryDto.IconUrl` có presigned URL (1 giờ). Nếu không có icon → `null`.

> **Bucket:** Cần thêm `"categories": "category-icons"` vào `MinioSettings.Buckets` trong `appsettings.json`.

---

## Out of Scope
- Soft delete (dùng hard delete — category không có `IsDeleted`)
- Move subtree (batch re-parent)
- Ordering/weight trong cùng level

---

## Technical Approach

### Domain
**Entity:** `Category` — ĐÃ CÓ (`Domain/Entities/Category.cs`)
- `Name`, `Description`, `ParentId`, `Parent`, `Children`, `Books`
- `Create(name, description, parentId)` — factory method
- `Update(name, description, parentId)` — kiểm tra self-parent, trả `Result`

**Cần thêm vào `Category` entity (cho F4 — icon):**
- `IconObjectKey` — `string?` property, nullable, lưu key MinIO (ví dụ `categories/2026/05/08/{guid}.png`)
- `UpdateIcon(objectKey)` — set `IconObjectKey`, trả `Result`
- `RemoveIcon()` — set `IconObjectKey = null`

**Cần thêm vào Domain:**
- `CategoryErrors` — ĐÃ CÓ `SelfParent`, `NotFound(id)`. Cần thêm:
  - `CircularReference` — `Error.Validation("Category.CircularReference", "Setting this parent would create a circular reference.")`
  - `HasChildren` — `Error.Conflict("Category.HasChildren", "Cannot delete a category that has child categories.")`
  - `HasBooks` — `Error.Conflict("Category.HasBooks", "Cannot delete a category that has associated books.")`
  - `ParentNotFound(id)` — `Error.NotFound("Category.ParentNotFound", $"Parent category '{id}' was not found.")`

**Business Invariants:**
- `parentId != Id` (self-parent) — đã có trong `Update()`
- Circular reference — kiểm tra trong Service (cần query DB để traverse ancestors)

### Application Layer — cấu trúc module

```
Application/
  Categories/
    IService/
      ICategoryQueryService.cs   ← GetById, GetPaged, GetTree, GetSubtree
      ICategoryCommandService.cs ← Create, Update, Delete, UploadIcon, DeleteIcon
    Services/
      CategoryQueryService.cs
      CategoryCommandService.cs
    Commands/
      CreateCategoryCommand.cs
      UpdateCategoryCommand.cs
      UploadCategoryIconCommand.cs   ← File (IFormFile), CategoryId (Guid), UploadedBy (Guid)
    Queries/
      GetCategoriesQuery.cs          ← kế thừa QueryParams
    DTOs/
      CategoryDto.cs                 ← flat (Id, Name, Description, ParentId, ParentName, IconUrl, ...)
      CategoryTreeDto.cs             ← nested (Id, Name, Description, IconUrl, Children: List<CategoryTreeDto>)
    CategoryErrors.cs
```

**Service methods:**

| Interface | Method | Mô tả |
|-----------|--------|-------|
| `ICategoryQueryService` | `GetByIdAsync(id, ct)` → `Result<CategoryDto>` | Lấy 1 category |
| | `GetPagedAsync(query, ct)` → `Result<PagedResult<CategoryDto>>` | Danh sách phân trang |
| | `GetTreeAsync(ct)` → `Result<List<CategoryTreeDto>>` | Toàn bộ cây (chỉ roots) |
| | `GetSubtreeAsync(id, ct)` → `Result<CategoryTreeDto>` | Subtree từ id |
| `ICategoryCommandService` | `CreateAsync(cmd, ct)` → `Result<Guid>` | Tạo mới |
| | `UpdateAsync(id, cmd, ct)` → `Result` | Cập nhật |
| | `DeleteAsync(id, ct)` → `Result` | Xóa vật lý |
| | `UploadIconAsync(cmd, ct)` → `Result<string>` | Upload icon, trả presigned URL |
| | `DeleteIconAsync(id, userId, ct)` → `Result` | Xóa icon khỏi MinIO + DB |

**Circular reference detection** — trong `CategoryCommandService`:
```
Khi update parentId:
  1. Nếu parentId == id → SelfParent (đã có trong domain)
  2. Load toàn bộ descendants của category hiện tại
  3. Nếu parentId nằm trong tập descendants → CircularReference
```

**Icon flow** — trong `CategoryCommandService`:
```
UploadIconAsync:
  1. Load category, NotFound nếu không tồn tại
  2. Gọi _mediaService.UploadAsync(UploadMediaCommand { File, Module = "categories", UploadedBy })
  3. Gọi category.UpdateIcon(mediaResult.Value.ObjectKey)
  4. SaveChangesAsync → trả presigned URL từ MediaDto.Url

DeleteIconAsync:
  1. Load category, NotFound nếu không tồn tại
  2. Nếu IconObjectKey == null → trả success (idempotent)
  3. Lookup Media record theo ObjectKey để lấy mediaId
  4. Gọi _mediaService.DeleteAsync(mediaId, userId, isAdmin)
  5. category.RemoveIcon() → SaveChangesAsync
```

### Infrastructure

**Repository Interface** (Application layer, Domain/IRepository):
```csharp
public interface ICategoryRepository
{
    Task<Category?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Category?> GetByIdWithChildrenAsync(Guid id, CancellationToken ct = default);
    Task<List<Category>> GetAllWithChildrenAsync(CancellationToken ct = default);
    Task<IQueryable<Category>> GetQueryableAsync();
    Task<bool> HasChildrenAsync(Guid id, CancellationToken ct = default);
    Task<bool> HasBooksAsync(Guid id, CancellationToken ct = default);
    Task<List<Guid>> GetDescendantIdsAsync(Guid id, CancellationToken ct = default);
    void Add(Category category);
    void Remove(Category category);
}
```

**EF Core config:** Cần thêm migration `AddCategoryIconObjectKey` để thêm column `IconObjectKey` (nvarchar(500), nullable).

**Repository Implementation** — `Infrastructure/Repository/CategoryRepository.cs`
- `GetDescendantIdsAsync`: Recursive CTE hoặc traversal in-memory (tập nhỏ — load all và traverse)
- `GetAllWithChildrenAsync`: Load tất cả category, EF Core tự build navigation graph

### API

| Method | Endpoint | Auth | Mô tả |
|--------|----------|------|-------|
| GET | `/api/categories` | AllowAnonymous | Danh sách phân trang |
| GET | `/api/categories/{id}` | AllowAnonymous | Chi tiết 1 category |
| GET | `/api/categories/tree` | AllowAnonymous | Toàn bộ cây |
| GET | `/api/categories/{id}/subtree` | AllowAnonymous | Subtree từ id |
| POST | `/api/categories` | Admin | Tạo category |
| PUT | `/api/categories/{id}` | Admin | Cập nhật category |
| DELETE | `/api/categories/{id}` | Admin | Xóa category |
| POST | `/api/categories/{id}/icon` | Admin | Upload icon (multipart/form-data) |
| DELETE | `/api/categories/{id}/icon` | Admin | Xóa icon |

**Controller:** `CategoriesController : BaseController`

---

## DTOs

```csharp
// Flat — dùng cho list và get by id
public record CategoryDto(
    Guid Id,
    string Name,
    string? Description,
    Guid? ParentId,
    string? ParentName,
    int ChildrenCount,
    string? IconUrl,         // presigned URL, null nếu không có icon
    DateTime CreatedAt,
    DateTime UpdatedAt
);

// Nested — dùng cho tree
public record CategoryTreeDto(
    Guid Id,
    string Name,
    string? Description,
    string? IconUrl,         // presigned URL, null nếu không có icon
    List<CategoryTreeDto> Children
);

// Commands
public record CreateCategoryCommand(string Name, string? Description, Guid? ParentId);
public record UpdateCategoryCommand(string Name, string? Description, Guid? ParentId);

// Query
public sealed class GetCategoriesQuery : QueryParams
{
    public Guid? ParentId { get; set; }  // filter theo parent
}
```

---

## Validation Plan

| Rule | Tầng | Công cụ |
|------|------|---------|
| `Name` required, max 100 chars | API | FluentValidation |
| `Description` max 500 chars | API | FluentValidation |
| `ParentId` nếu có, là valid Guid | API | FluentValidation (type check) |
| Self-parent (`parentId == id`) | Domain | `Category.Update()` → `CategoryErrors.SelfParent` |
| Parent tồn tại | Application | Service query DB → `CategoryErrors.ParentNotFound` |
| Circular reference | Application | Service traverse descendants → `CategoryErrors.CircularReference` |
| Delete với children | Application | Service query DB → `CategoryErrors.HasChildren` |
| Delete với books | Application | Service query DB → `CategoryErrors.HasBooks` |
| Icon: file required, MIME type, size | API | Tái dùng `UploadMediaRequestValidator` từ Media module |

**Validators:**
- `CreateCategoryCommandValidator` — `Name` NotEmpty + MaxLength(100), `Description` MaxLength(500)
- `UpdateCategoryCommandValidator` — tương tự
- Icon upload: tái dùng `UploadMediaRequestValidator` đã có sẵn trong API layer

---

## Testing Strategy

### Unit Test — CategoryCommandService
- `CreateAsync_ShouldReturnGuid_WhenValid` — happy path
- `CreateAsync_ShouldFail_WhenParentNotFound` — parentId không tồn tại
- `UpdateAsync_ShouldFail_WhenSelfParent` — parentId == id
- `UpdateAsync_ShouldFail_WhenCircularReference` — parentId là descendant
- `DeleteAsync_ShouldFail_WhenHasChildren` — category còn child
- `DeleteAsync_ShouldFail_WhenHasBooks` — category còn sách
- `UploadIconAsync_ShouldReturnUrl_WhenSuccess` — mock `IMediaService`
- `DeleteIconAsync_ShouldSucceed_WhenIconExists` — xóa icon
- `DeleteIconAsync_ShouldSucceed_WhenNoIcon` — idempotent khi không có icon

### Unit Test — CategoryQueryService
- `GetByIdAsync_ShouldReturnDto_WhenFound`
- `GetByIdAsync_ShouldReturnNotFound_WhenMissing`
- `GetTreeAsync_ShouldReturnOnlyRootCategories` — roots có populated Children

### Unit Test — Domain
- `Category_Update_ShouldFail_WhenParentIsSelf` — domain invariant
- `Category_UpdateIcon_ShouldSetObjectKey`
- `Category_RemoveIcon_ShouldSetNull`

**Mocks:** `ICategoryRepository`, `IUnitOfWork`, `IMediaService`

---

## Boundaries

### Always Do
- Result Pattern — không throw exception cho lỗi nghiệp vụ
- Error code format: `Category.{Action}` (`Category.NotFound`, `Category.CircularReference`)
- Controller chỉ gọi `HandleResult()` từ `BaseController`
- SOLID: tách `ICategoryQueryService` / `ICategoryCommandService` (ISP)
- Validation đúng tầng (format ở API, business rule ở Application, invariant ở Domain)
- Icon: lưu `IconObjectKey` vào DB, **không** lưu presigned URL — generate on-the-fly

### Ask First
- Thêm soft delete cho Category
- Thay đổi delete behavior (cascade vs restrict)
- Tăng giới hạn file size cho icon

### Never Do
- Circular reference check trong Domain (cần DB query — thuộc Application)
- Business logic trong Controller
- N+1 khi load tree (load all + build in-memory)
- Upload file MinIO trực tiếp trong CategoryCommandService — phải đi qua `IMediaService`
- Lưu presigned URL vào `IconObjectKey` — URL có expire, không bền vững
