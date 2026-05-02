---
name: spec
description: Tạo SPEC.md trước khi code — structured PRD cho feature mới của BookStore
---

# /spec — Specification-Driven Development

> "Plan the work, then work the plan."

## Workflow

### Phase 1: Discovery — Hỏi trước khi làm

**Scope**
- Objective của feature là gì?
- Module nào liên quan? (Books / Orders / Auth / Reviews / ...)
- Problem gì đang được giải quyết?

**Features**
- MVP gồm những gì?
- Acceptance criteria cho từng feature?
- Explicitly out of scope?

**Technical (BookStore-specific)**
- Cần thêm bảng DB / Entity mới không?
- Có relationship mới không? (1-N, N-N)
- Cần file upload qua MinIO không?
- Endpoint nào cần `[Authorize]`? Role nào? (Admin / Customer)
- Validation rule nào? Ở tầng nào? (FluentValidation / Business Rule / Domain Invariant)
- Có state machine không? (như Order)

---

### Phase 2: Generate SPEC.md

```markdown
# Feature: [Tên feature]

## Objective
[1-2 câu mô tả mục tiêu]

## Module
[Books / Orders / Auth / Authors / Categories / Reviews / Dashboard]

## Core Features
1. [Feature A] — Acceptance: [...]
2. [Feature B] — Acceptance: [...]

## Out of Scope
- [Những gì KHÔNG build trong iteration này]

## Technical Approach

### Domain
- Entity mới / thay đổi: [...]
- Business Invariants: [...]
- State machine (nếu có): [...]

### Application
- Service methods: [...]
- Commands / Queries: [...]
- DTOs: [...]
- Error class: `{Module}Errors.cs` — các error cần định nghĩa: [...]
- Validators (FluentValidation): [...]

### Infrastructure
- EF Core config + Migration: [...]
- MinIO (nếu có): [...]

### API
- Endpoints: [METHOD /api/resource]
- Auth: [Authorize] / [AllowAnonymous]
- Response: ApiResponse<T>

## Validation Plan
| Rule | Tầng | Công cụ |
|------|------|---------|
| Required, format | API | FluentValidation |
| Unique, exists | Application | Business rule in Service |
| Invariant | Domain | Private ctor / method |

## Testing Strategy
- Unit test Service: [success path, failure path]
- Unit test Domain: [state machine / invariant nếu có]
- Mock: IRepository liên quan

## Boundaries
### Always Do
- Dùng Result Pattern — không throw exception cho lỗi nghiệp vụ
- Error code format: `{Module}.{Action}` (vd: `Book.NotFound`)
- Controller chỉ gọi `result.ToActionResult()`
- Validation đúng tầng

### Ask First
- Thêm bảng DB / thay đổi schema lớn
- Thêm dependency / package mới
- Thay đổi Shared Layer

### Never Do
- Business logic trong Controller
- Domain Entity phụ thuộc Infrastructure / EF Core
- Shared import từ Application / Domain
- throw Exception cho lỗi nghiệp vụ
```

---

### Phase 3: Review & Confirm

- Present spec cho user
- Chờ confirm trước khi chạy `/plan`
- Lưu tại: `docs/specs/[feature-name].md`

## Output
- `docs/specs/[feature].md` — Specification document
- Alignment rõ ràng trước khi implement

## Next Step
Sau khi spec được approve → chạy `/plan` để decompose thành tasks.
