# TODO: Categories Module

> Spec: `docs/specs/categories-module.md`
> Branch: `feature/add-categories-module`

---

## Slice 1: Foundation + Create Category

**Objective**: Admin có thể gọi `POST /api/categories` và nhận 201 Created. Đây là slice foundation — thiết lập tất cả scaffolding mà các slice sau dùng chung.

### Files to create/modify

**Domain layer:**
- `BookStore.Domain/Errors/CategoryErrors.cs` *(modify)* — thêm 4 errors mới: `CircularReference`, `HasChildren`, `HasBooks`, `ParentNotFound(id)`
- `BookStore.Domain/IRepository/ICategoryRepository.cs` *(create)* — interface đầy đủ (tất cả methods, slice sau implement dần)

**Application layer:**
- `BookStore.Application/Categories/Commands/CreateCategoryCommand.cs` *(create)* — `record CreateCategoryCommand(string Name, string? Description, Guid? ParentId)`
- `BookStore.Application/Categories/DTOs/CategoryDto.cs` *(create)* — `record CategoryDto(Guid Id, string Name, string? Description, Guid? ParentId, string? ParentName, int ChildrenCount, DateTime CreatedAt, DateTime UpdatedAt)`
- `BookStore.Application/Categories/IService/ICategoryCommandService.cs` *(create)* — khai báo `CreateAsync`, `UpdateAsync`, `DeleteAsync`
- `BookStore.Application/Categories/Services/CategoryCommandService.cs` *(create)* — implement `CreateAsync` (check parent exists → Category.Create → Add → SaveChanges)

**Infrastructure layer:**
- `BookStore.Infrastructure/Repository/CategoryRepository.cs` *(create)* — implement `Add`, `GetByIdAsync`

**API layer:**
- `BookStore.API/Validators/CreateCategoryCommandValidator.cs` *(create)* — `Name` NotEmpty + MaxLength(100), `Description` MaxLength(500)
- `BookStore.API/Controllers/CategoriesController.cs` *(create)* — chỉ `POST /api/categories` endpoint
- `BookStore.API/Extensions/ServiceExtensions.cs` *(modify)* — đăng ký `ICategoryRepository`, `ICategoryCommandService`

**Tests:**
- `BookStore.Application.Tests/Application/Categories/CategoryCommandServiceTests.cs` *(create)*
  - `CreateAsync_ShouldReturnGuid_WhenValid`
  - `CreateAsync_ShouldFail_WhenParentNotFound`

### Acceptance Criteria
- [ ] `POST /api/categories` trả 201 + `CategoryDto` (dù chưa có GET, dùng Swagger để verify)
- [ ] `POST /api/categories` với `parentId` không tồn tại → 404 `Category.ParentNotFound`
- [ ] `Name` trống → 400 validation từ FluentValidation (trước khi vào service)
- [ ] `dotnet build` sạch, `dotnet test` pass

### Key implementation notes
```
CreateAsync flow:
  1. Nếu cmd.ParentId != null → _repo.GetByIdAsync(parentId) → nếu null → ParentNotFound
  2. var category = Category.Create(cmd.Name, cmd.Description, cmd.ParentId)
  3. _repo.Add(category)
  4. await _unitOfWork.SaveChangesAsync(ct)
  5. return category.Id
```

**Dependencies**: none — slice đầu tiên

---

## Checkpoint 1

- [ ] `dotnet build` không warning/error
- [ ] `dotnet test` — 2 test cases pass
- [ ] POST /api/categories hoạt động qua Swagger
- [ ] `CategoryErrors` có đủ 6 errors

---

## Slice 2: Read Category (Flat List + By ID)

**Objective**: Bất kỳ ai cũng có thể đọc danh sách category phân trang và xem chi tiết 1 category.

### Files to create/modify

**Application layer:**
- `BookStore.Application/Categories/Queries/GetCategoriesQuery.cs` *(create)* — kế thừa `QueryParams`, thêm `Guid? ParentId`
- `BookStore.Application/Categories/IService/ICategoryQueryService.cs` *(create)* — khai báo `GetByIdAsync`, `GetPagedAsync`, `GetTreeAsync`, `GetSubtreeAsync`
- `BookStore.Application/Categories/Services/CategoryQueryService.cs` *(create)* — implement `GetByIdAsync` + `GetPagedAsync`

**Infrastructure layer:**
- `BookStore.Infrastructure/Repository/CategoryRepository.cs` *(modify)* — thêm `GetByIdAsync` (with Parent include), `GetQueryable()`

**API layer:**
- `BookStore.API/Controllers/CategoriesController.cs` *(modify)* — thêm `GET /api/categories` + `GET /api/categories/{id}`
- `BookStore.API/Extensions/ServiceExtensions.cs` *(modify)* — đăng ký `ICategoryQueryService`

**Tests:**
- `BookStore.Application.Tests/Application/Categories/CategoryQueryServiceTests.cs` *(create)*
  - `GetByIdAsync_ShouldReturnCategoryDto_WhenFound`
  - `GetByIdAsync_ShouldReturnNotFound_WhenMissing`
  - `GetPagedAsync_ShouldReturnPagedResult_WhenCalled`

### Acceptance Criteria
- [ ] `GET /api/categories/{id}` → 200 `CategoryDto` với `ParentName` được populate
- [ ] `GET /api/categories/{id}` với id không tồn tại → 404 `Category.NotFound`
- [ ] `GET /api/categories?page=1&pageSize=10&searchTerm=tech` → 200 `PagedResult<CategoryDto>`
- [ ] `GET /api/categories?parentId=<guid>` → lọc theo parent
- [ ] `pageSize` > 50 → clamp về 50 (từ `PaginationParams`)

### Key implementation notes
```
GetByIdAsync flow:
  var category = await _repo.GetByIdAsync(id, ct);   // Include Parent
  if (category is null) return CategoryErrors.NotFound(id);
  return new CategoryDto(
      category.Id, category.Name, category.Description,
      category.ParentId, category.Parent?.Name,
      category.Children.Count,
      category.CreatedAt, category.UpdatedAt
  );

GetPagedAsync flow:
  var query = _repo.GetQueryable()
      .Include(c => c.Parent)
      .Include(c => c.Children)
      .Where(c => request.SearchTerm == null || c.Name.Contains(request.SearchTerm));
  if (request.ParentId.HasValue)
      query = query.Where(c => c.ParentId == request.ParentId);
  return await query
      .ApplySort(request.SortBy, request.IsAscending)
      .Select(c => new CategoryDto(...))
      .ToPagedResultAsync(request, ct);
```

**Dependencies**: Slice 1

---

## Checkpoint 2

- [ ] `dotnet build` sạch
- [ ] 5 test cases pass
- [ ] GET /api/categories + GET /api/categories/{id} hoạt động qua Swagger
- [ ] Không có N+1 (Include rõ ràng, không lazy loading)

---

## Slice 3: Update Category (với Circular Reference Detection)

**Objective**: Admin có thể sửa category. Service phát hiện circular reference trước khi lưu.

### Files to create/modify

**Application layer:**
- `BookStore.Application/Categories/Commands/UpdateCategoryCommand.cs` *(create)* — `record UpdateCategoryCommand(string Name, string? Description, Guid? ParentId)`
- `BookStore.Application/Categories/Services/CategoryCommandService.cs` *(modify)* — implement `UpdateAsync` với circular reference check

**Infrastructure layer:**
- `BookStore.Infrastructure/Repository/CategoryRepository.cs` *(modify)* — thêm `GetDescendantIdsAsync`

**API layer:**
- `BookStore.API/Validators/UpdateCategoryCommandValidator.cs` *(create)* — tương tự CreateValidator
- `BookStore.API/Controllers/CategoriesController.cs` *(modify)* — thêm `PUT /api/categories/{id}`

**Tests:**
- `BookStore.Application.Tests/Application/Categories/CategoryCommandServiceTests.cs` *(modify)*
  - `UpdateAsync_ShouldSucceed_WhenValid`
  - `UpdateAsync_ShouldFail_WhenCategoryNotFound`
  - `UpdateAsync_ShouldFail_WhenSelfParent` — domain guard
  - `UpdateAsync_ShouldFail_WhenCircularReference` — parentId là descendant

### Acceptance Criteria
- [ ] `PUT /api/categories/{id}` với data hợp lệ → 200
- [ ] `PUT /api/categories/{id}` với `parentId == id` → 400 `Category.SelfParent`
- [ ] `PUT /api/categories/{id}` với `parentId` là descendant → 400 `Category.CircularReference`
- [ ] `PUT /api/categories/{id}` với `parentId` không tồn tại → 404 `Category.ParentNotFound`

### Key implementation notes
```
UpdateAsync flow:
  1. Load category = _repo.GetByIdAsync(id) → NotFound nếu null
  2. Nếu cmd.ParentId có giá trị:
     a. Nếu cmd.ParentId != null → check parent tồn tại
     b. Gọi domain: result = category.Update(cmd.Name, cmd.Description, cmd.ParentId)
        → domain tự check SelfParent → trả Result.Failure(SelfParent) nếu vi phạm
     c. Nếu result.IsFailure → trả ngay
     d. Load descendantIds = _repo.GetDescendantIdsAsync(id)
        → Nếu cmd.ParentId nằm trong tập → CircularReference
  3. category.Update(cmd.Name, cmd.Description, cmd.ParentId)
  4. _unitOfWork.SaveChangesAsync(ct)

GetDescendantIdsAsync — load in-memory, traverse BFS:
  var all = await _context.Categories.ToListAsync(ct);
  var result = new List<Guid>();
  var queue = new Queue<Guid>([rootId]);
  while (queue.TryDequeue(out var current))
      foreach (var child in all.Where(c => c.ParentId == current))
      { result.Add(child.Id); queue.Enqueue(child.Id); }
  return result;
```

**Dependencies**: Slice 1, Slice 2

---

## Checkpoint 3

- [ ] `dotnet build` sạch
- [ ] 9 test cases pass
- [ ] PUT /api/categories/{id} hoạt động, circular reference bị chặn đúng

---

## Slice 4: Delete Category (với HasChildren / HasBooks Guards)

**Objective**: Admin xóa được category. Service từ chối nếu còn children hoặc còn sách.

### Files to create/modify

**Application layer:**
- `BookStore.Application/Categories/Services/CategoryCommandService.cs` *(modify)* — implement `DeleteAsync`

**Infrastructure layer:**
- `BookStore.Infrastructure/Repository/CategoryRepository.cs` *(modify)* — thêm `HasChildrenAsync`, `HasBooksAsync`, `Remove`

**API layer:**
- `BookStore.API/Controllers/CategoriesController.cs` *(modify)* — thêm `DELETE /api/categories/{id}`

**Tests:**
- `BookStore.Application.Tests/Application/Categories/CategoryCommandServiceTests.cs` *(modify)*
  - `DeleteAsync_ShouldSucceed_WhenCategoryIsLeafAndEmpty`
  - `DeleteAsync_ShouldFail_WhenCategoryNotFound`
  - `DeleteAsync_ShouldFail_WhenHasChildren` — 409 `Category.HasChildren`
  - `DeleteAsync_ShouldFail_WhenHasBooks` — 409 `Category.HasBooks`

### Acceptance Criteria
- [ ] `DELETE /api/categories/{id}` trả 200 (dùng `HandleResult(Result)` từ BaseController)
- [ ] Xóa category còn child → 409 `Category.HasChildren`
- [ ] Xóa category còn sách → 409 `Category.HasBooks`
- [ ] Xóa category không tồn tại → 404 `Category.NotFound`

### Key implementation notes
```
DeleteAsync flow:
  1. var category = _repo.GetByIdAsync(id) → NotFound nếu null
  2. if (await _repo.HasChildrenAsync(id, ct)) return CategoryErrors.HasChildren
  3. if (await _repo.HasBooksAsync(id, ct)) return CategoryErrors.HasBooks
  4. _repo.Remove(category)
  5. await _unitOfWork.SaveChangesAsync(ct)

HasChildrenAsync:
  return await _context.Categories.AnyAsync(c => c.ParentId == id, ct);

HasBooksAsync:
  return await _context.Books.AnyAsync(b => b.CategoryId == id, ct);
```

**Note:** `DELETE /api/categories/{id}` trả `200 OK` (có body `ApiResponse`), không phải `204 No Content`. Dùng `HandleResult(Result result)` từ `BaseController`.

**Dependencies**: Slice 1

---

## Checkpoint 4

- [ ] `dotnet build` sạch
- [ ] 13 test cases pass
- [ ] Full CRUD hoạt động
- [ ] Không thể xóa category có children hoặc books

---

## Slice 5: Tree API

**Objective**: Bất kỳ ai cũng có thể xem toàn bộ cây category dạng nested JSON.

### Files to create/modify

**Application layer:**
- `BookStore.Application/Categories/DTOs/CategoryTreeDto.cs` *(create)* — `record CategoryTreeDto(Guid Id, string Name, string? Description, List<CategoryTreeDto> Children)`
- `BookStore.Application/Categories/Services/CategoryQueryService.cs` *(modify)* — implement `GetTreeAsync` + `GetSubtreeAsync`

**Infrastructure layer:**
- `BookStore.Infrastructure/Repository/CategoryRepository.cs` *(modify)* — thêm `GetAllWithChildrenAsync`, `GetByIdWithChildrenAsync`

**API layer:**
- `BookStore.API/Controllers/CategoriesController.cs` *(modify)* — thêm `GET /api/categories/tree` + `GET /api/categories/{id}/subtree`

**Tests:**
- `BookStore.Application.Tests/Application/Categories/CategoryQueryServiceTests.cs` *(modify)*
  - `GetTreeAsync_ShouldReturnOnlyRootCategories_WithNestedChildren`
  - `GetSubtreeAsync_ShouldReturnSubtree_WhenCategoryFound`
  - `GetSubtreeAsync_ShouldReturnNotFound_WhenCategoryMissing`

### Acceptance Criteria
- [ ] `GET /api/categories/tree` → `List<CategoryTreeDto>` chỉ gồm root categories (ParentId == null), Children nested đúng
- [ ] `GET /api/categories/{id}/subtree` → `CategoryTreeDto` bắt đầu từ category id, Children nested đúng
- [ ] Không có N+1 — tất cả load 1 query, build tree in-memory
- [ ] Category không tồn tại → 404

### Key implementation notes
```
GetAllWithChildrenAsync:
  // EF Core tự populate navigation khi load toàn bộ cùng 1 context instance
  return await _context.Categories.ToListAsync(ct);
  // Không cần .Include() — EF fix-up tự động nếu load tất cả entities

GetTreeAsync flow:
  var all = await _repo.GetAllWithChildrenAsync(ct);
  var roots = all.Where(c => c.ParentId == null).ToList();
  return roots.Select(r => BuildTreeDto(r)).ToList();

BuildTreeDto (recursive helper — private trong service):
  private static CategoryTreeDto BuildTreeDto(Category c) =>
      new(c.Id, c.Name, c.Description,
          c.Children.Select(BuildTreeDto).ToList());

GetSubtreeAsync flow:
  var category = await _repo.GetAllWithChildrenAsync(ct);
  var root = category.FirstOrDefault(c => c.Id == id);
  if (root is null) return CategoryErrors.NotFound(id);
  return BuildTreeDto(root);
```

**Route conflict note:** `GET /api/categories/tree` và `GET /api/categories/{id}` có thể conflict — đặt `[HttpGet("tree")]` TRƯỚC `[HttpGet("{id:guid}")]` trong controller, hoặc dùng `:guid` constraint để ASP.NET Core phân biệt.

**Dependencies**: Slice 2

---

## Checkpoint 5 — Final

- [ ] `dotnet build` không warning
- [ ] `dotnet test` — 16 test cases pass (2+3+4+4+3)
- [ ] Tất cả 7 endpoints hoạt động qua Swagger
- [ ] Route `GET /api/categories/tree` không conflict với `GET /api/categories/{id:guid}`
- [ ] Tree response đúng cấu trúc nested JSON
- [ ] SOLID checklist:
  - [ ] SRP: `CategoryQueryService` / `CategoryCommandService` tách biệt
  - [ ] ISP: `ICategoryQueryService` / `ICategoryCommandService` tách biệt
  - [ ] DIP: không `new` dependency trong service
  - [ ] Không có business logic trong Controller

---

## Tổng kết files

| File | Slice | Action |
|------|-------|--------|
| `Domain/Errors/CategoryErrors.cs` | 1 | modify |
| `Domain/IRepository/ICategoryRepository.cs` | 1 | create |
| `Application/Categories/Commands/CreateCategoryCommand.cs` | 1 | create |
| `Application/Categories/DTOs/CategoryDto.cs` | 1 | create |
| `Application/Categories/IService/ICategoryCommandService.cs` | 1 | create |
| `Application/Categories/Services/CategoryCommandService.cs` | 1,3,4 | create→modify |
| `Infrastructure/Repository/CategoryRepository.cs` | 1,2,3,4,5 | create→modify |
| `API/Validators/CreateCategoryCommandValidator.cs` | 1 | create |
| `API/Controllers/CategoriesController.cs` | 1,2,3,4,5 | create→modify |
| `API/Extensions/ServiceExtensions.cs` | 1,2 | modify |
| `Application/Categories/Queries/GetCategoriesQuery.cs` | 2 | create |
| `Application/Categories/IService/ICategoryQueryService.cs` | 2 | create |
| `Application/Categories/Services/CategoryQueryService.cs` | 2,5 | create→modify |
| `Application/Categories/Commands/UpdateCategoryCommand.cs` | 3 | create |
| `API/Validators/UpdateCategoryCommandValidator.cs` | 3 | create |
| `Application/Categories/DTOs/CategoryTreeDto.cs` | 5 | create |
| `Tests/.../CategoryCommandServiceTests.cs` | 1,3,4 | create→modify |
| `Tests/.../CategoryQueryServiceTests.cs` | 2,5 | create→modify |
