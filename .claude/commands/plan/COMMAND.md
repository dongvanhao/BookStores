---
name: plan
description: Decompose spec thành tasks nhỏ, có thứ tự, theo Clean Architecture layers
---

# /plan — Planning & Task Breakdown

> "Vertical slices, not horizontal layers."

## Prerequisites
- Spec đã được approve (`docs/specs/[feature].md` hoặc mô tả rõ yêu cầu)
- Hiểu cấu trúc Clean Architecture của dự án

## Workflow

### Phase 1: Analysis (Read-Only)
1. Đọc spec — objective + acceptance criteria
2. Khảo sát codebase — file liên quan, pattern hiện có
3. Map dependency — tầng nào phụ thuộc tầng nào?

> **Không sửa code trong Phase này.**

---

### Phase 2: Vertical Slicing

```
❌ Horizontal (anti-pattern):
   Task 1: Tạo tất cả Entity + Migration
   Task 2: Tạo tất cả Repository
   Task 3: Tạo tất cả Controller

✅ Vertical (đúng — Clean Architecture):
   Task 1: User có thể tạo sách (Domain → Application → API)
   Task 2: User có thể xem danh sách sách (paging, filter)
   Task 3: Admin có thể xóa sách (soft delete + auth)
```

---

### Phase 3: Task Definition

```markdown
## Task [X.Y]: [Mô tả ngắn]

**Objective**: [Feature này deliver gì?]

**Files to create/modify**:
- `BookStore.Domain/Books/Book.cs`
- `BookStore.Application/Books/BookErrors.cs`
- `BookStore.Application/Books/Commands/CreateBookCommand.cs`
- `BookStore.Application/Books/Validators/CreateBookCommandValidator.cs`
- `BookStore.Application/Books/BookService.cs`
- `BookStore.Infrastructure/Repositories/BookRepository.cs`
- `BookStore.Infrastructure/Persistence/Configurations/BookConfiguration.cs`
- `BookStore.API/Controllers/BooksController.cs`
- `BookStore.UnitTests/Application/Books/BookServiceTests.cs`

**Acceptance Criteria**:
- [ ] Entity tạo đúng qua factory method (private ctor)
- [ ] Service trả về `Result<T>`, không throw exception
- [ ] Controller chỉ gọi `result.ToActionResult()`
- [ ] FluentValidation chặn input sai ở API layer
- [ ] Business rule validate ở Application layer (mock repository)
- [ ] Unit test cover Success + Failure path
- [ ] EF Core config + Migration đúng

**Dependencies**: [Task IDs cần hoàn thành trước]

**Verification**:
- [ ] `dotnet build` không có warning/error
- [ ] `dotnet test` pass
- [ ] Swagger endpoint trả response đúng format `ApiResponse<T>`
```

---

### Phase 4: Ordering

Thứ tự chuẩn cho mỗi module mới:

1. **Domain** — Entity, Invariants, State machine
2. **Shared / Errors** — `{Module}Errors.cs`
3. **Application** — Interface, DTOs, Commands/Queries, Validators, Service
4. **Infrastructure** — EF Config, Migration, Repository implementation
5. **API** — Controller, đăng ký DI
6. **Tests** — Unit test Service + Domain

> Risk-first: Implement state machine / complex domain logic trước.

---

### Phase 5: Checkpoints

```markdown
---
## Checkpoint: [Milestone name]

**Verify trước khi tiếp tục**:
- [ ] `dotnet build` sạch
- [ ] `dotnet test` pass
- [ ] Dependency rule không bị vi phạm (Domain không import gì ngoài)
- [ ] Swagger response đúng `ApiResponse<T>`
- [ ] Result Pattern: không có `throw` cho lỗi nghiệp vụ

---
```

## Output

Lưu tại `docs/tasks/`:

```markdown
# TODO: [Feature Name]

## Phase 1: Domain + Errors
- [ ] Task 1.1: Tạo Entity + factory method
- [ ] Task 1.2: Tạo {Module}Errors.cs

## Checkpoint: Domain complete

## Phase 2: Application Layer
- [ ] Task 2.1: Interface + DTOs
- [ ] Task 2.2: Validator (FluentValidation)
- [ ] Task 2.3: Service (Result Pattern)

## Checkpoint: Application complete

## Phase 3: Infrastructure + API
- [ ] Task 3.1: EF Config + Migration
- [ ] Task 3.2: Repository implementation
- [ ] Task 3.3: Controller + DI registration

## Checkpoint: API complete

## Phase 4: Tests
- [ ] Task 4.1: Unit test Service
- [ ] Task 4.2: Unit test Domain (nếu có state machine)
```

## Next Step
Sau khi plan được approve → chạy `/build` để implement từng task.
