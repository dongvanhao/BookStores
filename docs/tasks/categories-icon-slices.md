# TODO: Categories Module — Vertical Slices

> **Baseline:** CRUD core (entity, repository, EF config, controller, application services) đã hoàn thành theo commit `feat(category): done api module categories`.
> 
> **Còn lại:** Feature 4 — Icon Upload + presigned URL on-the-fly + unit tests toàn module.

---

## Slice 1: Domain — IconObjectKey + category bucket config

**Objective:** Category có thể lưu icon object key; MinIO biết bucket nào dùng cho categories.

**Files to modify:**
- `src/BE/Core/BookStore.Domain/Entities/Category.cs`
- `src/BE/Shared/BookStore.Shared/Settings/MinioSettings.cs` *(hoặc `appsettings.json`)*

**Tasks:**
- [ ] **1.1** Thêm `IconObjectKey` (`string?`) property vào `Category` entity
- [ ] **1.2** Thêm `UpdateIcon(string objectKey) → Result` vào `Category`
- [ ] **1.3** Thêm `RemoveIcon() → void` vào `Category`
- [ ] **1.4** Thêm `"categories": "category-icons"` vào `MinioSettings.Buckets` trong `appsettings.json` + `appsettings.Development.json`

**Acceptance Criteria:**
- [ ] `Category.UpdateIcon("key")` → `IconObjectKey` được set
- [ ] `Category.RemoveIcon()` → `IconObjectKey == null`
- [ ] `dotnet build` không warning

**Dependencies:** Không có — làm trước tiên.

---

## Checkpoint: Domain complete
- [ ] `dotnet build` sạch
- [ ] Domain không import tầng nào khác

---

## Slice 2: Infrastructure — Migration + presigned URL trong CategoryDto

**Objective:** DB có column `IconObjectKey`; `GetByIdAsync` và `GetPagedAsync` trả `IconUrl` (presigned URL) khi category có icon.

**Files to create/modify:**
- `src/BE/Core/BookStore.Infrastructure/Migrations/` ← migration mới `AddCategoryIconObjectKey`
- `src/BE/Core/BookStore.Infrastructure/Data/Configurations/CategoryConfiguration.cs`
- `src/BE/Core/BookStore.Application/Categories/Services/CategoryQueryService.cs`
- `src/BE/Core/BookStore.Application/Categories/DTOs/CategoryDto.cs`
- `src/BE/Core/BookStore.Application/Categories/DTOs/CategoryTreeDto.cs`

**Tasks:**
- [ ] **2.1** Thêm `IconObjectKey nvarchar(500) NULL` vào `CategoryConfiguration` và tạo migration `AddCategoryIconObjectKey`
- [ ] **2.2** Inject `IMinioStorageService` vào `CategoryQueryService`
- [ ] **2.3** Trong mapping `Category → CategoryDto`: nếu `IconObjectKey != null` → gọi `_minioStorageService.GetPresignedUrlAsync(bucket, key, 1 giờ)`, ngược lại `IconUrl = null`
- [ ] **2.4** Áp dụng tương tự cho `CategoryTreeDto` mapping

**Acceptance Criteria:**
- [ ] `dotnet ef database update` thành công — column `IconObjectKey` xuất hiện
- [ ] `GET /api/categories/{id}` với category không có icon → `iconUrl: null`
- [ ] `GET /api/categories/{id}` với category có icon → `iconUrl` chứa presigned URL (1 giờ)

**Dependencies:** Slice 1 (IconObjectKey trên entity)

---

## Checkpoint: Infrastructure + presigned URL complete
- [ ] Migration apply thành công
- [ ] `dotnet build` sạch
- [ ] `GetById` trả `iconUrl` đúng

---

## Slice 3: Application — UploadIcon + DeleteIcon service methods

**Objective:** Service có thể upload/xóa icon cho category, đi qua `IMediaService`.

**Files to create/modify:**
- `src/BE/Core/BookStore.Application/Categories/Commands/UploadCategoryIconCommand.cs` ← **tạo mới**
- `src/BE/Core/BookStore.Application/Categories/IService/ICategoryCommandService.cs`
- `src/BE/Core/BookStore.Application/Categories/Services/CategoryCommandService.cs`

**Tasks:**
- [ ] **3.1** Tạo `UploadCategoryIconCommand(IFormFile File, Guid CategoryId, Guid UploadedBy)`
- [ ] **3.2** Thêm vào `ICategoryCommandService`:
  ```csharp
  Task<Result<string>> UploadIconAsync(UploadCategoryIconCommand cmd, CancellationToken ct = default);
  Task<Result>         DeleteIconAsync(Guid categoryId, Guid userId, bool isAdmin, CancellationToken ct = default);
  ```
- [ ] **3.3** Implement `UploadIconAsync` trong `CategoryCommandService`:
  - Load category → `NotFound` nếu không có
  - Gọi `_mediaService.UploadAsync(new UploadMediaCommand { File, Module = "categories", UploadedBy })`
  - `category.UpdateIcon(mediaResult.Value.ObjectKey)`
  - `SaveChangesAsync` → trả `mediaResult.Value.Url` (presigned URL từ MediaDto)
- [ ] **3.4** Implement `DeleteIconAsync` trong `CategoryCommandService`:
  - Load category → `NotFound` nếu không có
  - Nếu `IconObjectKey == null` → trả `Result.Success()` (idempotent)
  - Lookup `Media` record theo `ObjectKey` để lấy `mediaId`
  - Gọi `_mediaService.DeleteAsync(mediaId, userId, isAdmin)`
  - `category.RemoveIcon()` → `SaveChangesAsync`

**Acceptance Criteria:**
- [ ] Upload: `CategoryDto.IconUrl` có presigned URL sau khi upload
- [ ] Delete: idempotent — gọi lại khi không có icon vẫn trả success
- [ ] Service không throw exception — dùng Result Pattern

**Dependencies:** Slice 2 (migration, entity methods)

---

## Checkpoint: Application layer complete
- [ ] `dotnet build` sạch
- [ ] Không có `throw` cho lỗi nghiệp vụ
- [ ] ISP: `ICategoryCommandService` không quá 6 method

---

## Slice 4: API — Icon endpoints trong CategoriesController

**Objective:** Expose 2 endpoint icon qua HTTP; validation file ở API layer.

**Files to modify:**
- `src/BE/Core/BookStore.API/Controllers/CategoriesController.cs`
- `src/BE/Core/BookStore.API/Validators/UploadCategoryIconCommandValidator.cs` ← **tạo mới** (hoặc tái dùng `UploadMediaRequestValidator`)

**Tasks:**
- [ ] **4.1** Tạo validator cho icon upload:
  - Tái dùng `UploadMediaRequestValidator` nếu đã có sẵn
  - Hoặc tạo `UploadCategoryIconCommandValidator`: `File` required, MIME type (`image/jpeg`, `image/png`, `image/webp`), max size 2MB
- [ ] **4.2** Thêm endpoint `POST /api/categories/{id:guid}/icon` vào controller:
  ```csharp
  /// <summary>Upload icon cho category.</summary>
  [HttpPost("{id:guid}/icon"), Authorize(Roles = "Admin")]
  [ProducesResponseType(typeof(ApiResponse<string>), 200)]
  public async Task<IActionResult> UploadIcon(Guid id, [FromForm] IFormFile file, CancellationToken ct)
  ```
- [ ] **4.3** Thêm endpoint `DELETE /api/categories/{id:guid}/icon` vào controller:
  ```csharp
  /// <summary>Xóa icon của category.</summary>
  [HttpDelete("{id:guid}/icon"), Authorize(Roles = "Admin")]
  [ProducesResponseType(204)]
  public async Task<IActionResult> DeleteIcon(Guid id, CancellationToken ct)
  ```
- [ ] **4.4** Đăng ký DI nếu chưa có trong `ServiceExtensions` / `InfrastructureExtensions`

**Acceptance Criteria:**
- [ ] Swagger hiển thị 2 endpoint mới với `multipart/form-data` cho upload
- [ ] `POST /api/categories/{id}/icon` với file hợp lệ → HTTP 200, `data` là presigned URL
- [ ] `DELETE /api/categories/{id}/icon` → HTTP 204
- [ ] File sai type/quá lớn → HTTP 400 từ FluentValidation

**Dependencies:** Slice 3

---

## Checkpoint: API complete
- [ ] `dotnet build` sạch
- [ ] Swagger response đúng `ApiResponse<T>`
- [ ] `GET /api/categories/{id}` trả `iconUrl` sau khi upload
- [ ] Controller không có business logic — chỉ gọi `result.ToActionResult()`

---

## Slice 5: Tests — Unit tests toàn module

**Objective:** Cover tất cả happy path + failure path theo spec.

**Files to create:**
- `tests/BookStore.UnitTests/Application/Categories/CategoryCommandServiceTests.cs`
- `tests/BookStore.UnitTests/Application/Categories/CategoryQueryServiceTests.cs`
- `tests/BookStore.UnitTests/Domain/Categories/CategoryTests.cs`

**Tasks:**

### Domain tests (`CategoryTests.cs`)
- [ ] **5.1** `Update_ShouldFail_WhenParentIsSelf` — `parentId == id`
- [ ] **5.2** `UpdateIcon_ShouldSetIconObjectKey` — `category.UpdateIcon("key")` → `IconObjectKey == "key"`
- [ ] **5.3** `RemoveIcon_ShouldSetIconObjectKeyToNull`

### CommandService tests (`CategoryCommandServiceTests.cs`)
- [ ] **5.4** `CreateAsync_ShouldReturnGuid_WhenValid` — happy path
- [ ] **5.5** `CreateAsync_ShouldFail_WhenParentNotFound` — `parentId` không tồn tại
- [ ] **5.6** `UpdateAsync_ShouldFail_WhenSelfParent` — `parentId == id`
- [ ] **5.7** `UpdateAsync_ShouldFail_WhenCircularReference` — `parentId` là descendant
- [ ] **5.8** `DeleteAsync_ShouldFail_WhenHasChildren` — category còn child
- [ ] **5.9** `DeleteAsync_ShouldFail_WhenHasBooks` — category còn sách
- [ ] **5.10** `UploadIconAsync_ShouldReturnPresignedUrl_WhenSuccess` — mock `IMediaService`
- [ ] **5.11** `DeleteIconAsync_ShouldSucceed_WhenIconExists`
- [ ] **5.12** `DeleteIconAsync_ShouldSucceed_WhenNoIcon` — idempotent

### QueryService tests (`CategoryQueryServiceTests.cs`)
- [ ] **5.13** `GetByIdAsync_ShouldReturnDto_WhenFound`
- [ ] **5.14** `GetByIdAsync_ShouldReturnNotFound_WhenMissing`
- [ ] **5.15** `GetTreeAsync_ShouldReturnOnlyRootCategories` — roots có populated Children

**Mocks:** `ICategoryRepository`, `IUnitOfWork`, `IMediaService`, `IMinioStorageService`

**Acceptance Criteria:**
- [ ] `dotnet test` — tất cả 15 test pass
- [ ] Không mock Service khác — chỉ mock ở boundary (repository, external service)

**Dependencies:** Slice 1–4

---

## Checkpoint: Module complete
- [ ] `dotnet build` sạch (0 warning)
- [ ] `dotnet test` pass (tất cả unit tests)
- [ ] Dependency rule: Domain không import tầng nào ngoài
- [ ] Result Pattern: không `throw` cho lỗi nghiệp vụ
- [ ] `GET /api/categories/{id}` → `iconUrl` đúng (presigned hoặc null)
- [ ] Swagger: 9 endpoints đúng format `ApiResponse<T>`

---

## Summary

| Slice | Phạm vi | Status |
|-------|---------|--------|
| CRUD core (entity, repo, controller, services) | Domain + App + Infra + API | ✅ Done |
| **Slice 1** | Domain: `IconObjectKey` + MinIO bucket config | ✅ Done |
| **Slice 2** | Infra: Migration `AddCategoryIconFields` + presigned URL mapping | ✅ Done |
| **Slice 3** | App: `UploadIconAsync` + `DeleteIconAsync` | ✅ Done |
| **Slice 4** | API: `POST/DELETE /{id}/icon` endpoints | ✅ Done |
| **Slice 5** | Tests: 22 unit tests (6 domain + 11 command + 5 query) | ✅ Done |
