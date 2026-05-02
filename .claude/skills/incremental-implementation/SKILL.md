---
name: Incremental Implementation
description: Build theo vertical slice — từng increment đều buildable và testable
---

# Incremental Implementation Skill — BookStore (.NET)

> "The simplest thing that could work."

## When to Apply

**Dùng khi:**
- Thêm module / feature mới (multi-layer changes)
- Refactor lớn (nhiều file)
- Bất kỳ thay đổi > 100 dòng

**Bỏ qua khi:**
- Fix nhỏ 1 file
- Config update
- Rename / reformat đơn giản

---

## Increment Cycle

```
1. Pick smallest complete piece
         ↓
2. Write failing test (RED)
         ↓
3. Implement minimal code (GREEN)
         ↓
4. Refactor nếu cần
         ↓
5. dotnet test — all pass
         ↓
6. git commit (atomic)
         ↓
7. Repeat cho piece tiếp theo
```

---

## Vertical vs Horizontal Slicing

### ✅ Vertical (đúng — Clean Architecture)

Mỗi slice deliver end-to-end qua tất cả tầng:

```
Slice 1: Admin có thể tạo sách
  └── Book.Create() (Domain)
  └── BookErrors.cs (Application)
  └── CreateBookCommandValidator (Application)
  └── BookService.CreateAsync() (Application)
  └── BookRepository.Add() (Infrastructure)
  └── BookConfiguration + Migration (Infrastructure)
  └── POST /api/books (API)
  └── Unit test: Service + Domain

Slice 2: User có thể xem danh sách sách (paging + filter)
  └── GetBooksQuery : QueryParams (Application)
  └── BookService.GetAllAsync() + Repository query (Application + Infrastructure)
  └── GET /api/books?page=1&pageSize=20 (API)
  └── Unit test: Service

Slice 3: Admin có thể xóa sách (soft delete)
  └── Book.Delete() domain method (Domain)
  └── Global Query Filter: IsDeleted (Infrastructure)
  └── DELETE /api/books/{id} (API)
  └── Unit test: soft delete behavior
```

### ❌ Horizontal (anti-pattern)

```
Task 1: Tạo tất cả Entity + Migration
Task 2: Tạo tất cả Repository
Task 3: Tạo tất cả Service
Task 4: Tạo tất cả Controller

❌ Không có gì hoạt động cho đến khi xong hết
```

---

## Slicing Strategies

### Happy Path First
```
Slice 1: Flow cơ bản hoạt động (tạo/đọc)
Slice 2: Business rule validation (duplicate, not found)
Slice 3: Auth + Authorization
Slice 4: Edge cases + error paths
```

### Risk-First
```
Slice 1: Domain Invariant / State machine (phức tạp nhất)
Slice 2: Application Service (phụ thuộc Domain đã verified)
Slice 3: Infrastructure + API (build trên nền đã ổn định)
```

---

## Rules

### 100-Line Rule
> Viết > 100 dòng mà chưa chạy test → dừng lại, verify.

### Touch Only What's Needed
> Không refactor code ngoài scope. Không thêm feature chưa được yêu cầu.

### Keep It Building
```bash
# Sau mỗi increment
dotnet build    # ✅ phải sạch
dotnet test tests/BookStore.UnitTests  # ✅ phải pass
```

### Rollback-Friendly
```bash
# Mỗi commit độc lập — revert được nếu cần
git revert HEAD
```

---

## Commit Strategy

**1 increment = 1 commit — atomic, focused:**

```bash
# ✅ Đúng — từng bước rõ ràng
git commit -m "feat(books): add Book domain entity with factory method"
git commit -m "feat(books): add BookErrors with NotFound and TitleExists"
git commit -m "feat(books): add CreateBookCommandValidator"
git commit -m "feat(books): implement BookService.CreateAsync with Result Pattern"
git commit -m "feat(books): add BookRepository and EF Core configuration"
git commit -m "feat(books): add POST /api/books endpoint"
git commit -m "test(books): add unit tests for BookService.CreateAsync"

# ❌ Sai — commit quá lớn
git commit -m "Add books feature"  # 500 dòng, 10 file
```

**Conventional Commits format:**
```
feat({module}): {mô tả}
fix({module}): {mô tả}
test({module}): {mô tả}
refactor({module}): {mô tả}
chore: {mô tả}
```

---

## Red Flags

Dừng lại nếu:
- Viết > 100 dòng chưa test
- Mix nhiều feature/task trong 1 commit
- Scope bị mở rộng giữa chừng
- `dotnet build` hoặc `dotnet test` fail giữa các increment
- Tạo abstraction "cho sau này" (YAGNI)
- Sửa file ngoài scope của task

---

## Verification Checklist

Sau mỗi increment:
```markdown
- [ ] dotnet build sạch (không warning/error)
- [ ] dotnet test pass
- [ ] Dependency rule không bị vi phạm
- [ ] Result Pattern đúng — không throw exception
- [ ] Commit atomic với message rõ ràng
- [ ] todo.md được update: - [x] Task hoàn thành
```
