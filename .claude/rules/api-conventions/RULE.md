# API Conventions — BookStore (.NET)

## URL Structure
- **kebab-case**: `/api/books`, `/api/book-categories`
- **Plural nouns**: `/api/books`, `/api/authors`, `/api/orders`
- **Nested resources**: `/api/orders/{id}/items`
- **No version prefix** (single-version project)

## HTTP Methods
| Method | Usage |
|--------|-------|
| GET | Read (idempotent) |
| POST | Create |
| PUT | Replace toàn bộ resource |
| PATCH | Cập nhật một phần |
| DELETE | Xóa |

## HTTP Status Codes — Map từ `ErrorType`
| Code | `ErrorType` | Khi nào |
|------|-------------|---------|
| 200 | — | GET/PUT/PATCH thành công |
| 201 | — | POST tạo resource thành công |
| 204 | — | DELETE thành công |
| 400 | `Validation` | FluentValidation fail |
| 401 | `Unauthorized` | Chưa xác thực / token hết hạn |
| 403 | — | Không đủ quyền (Admin required) |
| 404 | `NotFound` | Resource không tồn tại |
| 409 | `Conflict` | Trùng lặp (title, email,...) |
| 500 | `Failure` | Lỗi hệ thống không mong đợi |

> `ResultExtensions.ToActionResult()` tự động map `ErrorType` → status code. Controller không tự set status.

## Response Format — `ApiResponse<T>`

```json
// Thành công
{ "success": true, "data": { ... }, "message": null }

// Lỗi nghiệp vụ
{ "success": false, "data": null, "message": "Book with id '...' was not found.", "errorCode": "Book.NotFound" }

// Lỗi validation (HTTP 400)
{
  "success": false,
  "message": "Validation failed.",
  "errors": {
    "Title": ["Title is required."],
    "Price": ["Price must be greater than zero."]
  }
}

// Danh sách phân trang
{
  "success": true,
  "data": {
    "items": [ ... ],
    "page": 1,
    "pageSize": 20,
    "totalCount": 100,
    "totalPages": 5,
    "hasNextPage": true,
    "hasPrevPage": false
  }
}
```

## Filtering & Pagination
```
GET /api/books?page=1&pageSize=20&sortBy=createdAt&isAscending=false
GET /api/books?searchTerm=clean&categoryId=...&minPrice=50&maxPrice=200
```
- `pageSize` tối đa **50** (enforce trong `PaginationParams`)
- Query params dùng **camelCase**
- Params kế thừa từ `QueryParams` base class

## Controller Pattern
```csharp
// ✅ Chuẩn — mọi action đều dùng ToActionResult()
[HttpGet("{id:guid}")]
public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
{
    var result = await _bookService.GetByIdAsync(id, ct);
    return result.ToActionResult();
}

[HttpPost]
public async Task<IActionResult> Create(CreateBookRequest request, CancellationToken ct)
{
    var result = await _bookService.CreateAsync(request, ct);
    return result.ToActionResult(201); // trả 201 Created khi thành công
}
```

## Swagger / XML Doc
- Mọi endpoint PHẢI có XML doc comment để Swashbuckle generate đúng:

```csharp
/// <summary>Lấy thông tin sách theo ID.</summary>
/// <param name="id">Book GUID</param>
/// <response code="200">Trả về BookDto</response>
/// <response code="404">Không tìm thấy sách</response>
[ProducesResponseType(typeof(ApiResponse<BookDto>), 200)]
[ProducesResponseType(typeof(ApiResponse<BookDto>), 404)]
[HttpGet("{id:guid}")]
public async Task<IActionResult> GetById(Guid id, CancellationToken ct) { ... }
```

## Naming — Request/Response
| Thành phần | Pattern | Ví dụ |
|------------|---------|-------|
| Request DTO | `{Action}{Resource}Request` | `CreateBookRequest`, `UpdateBookRequest` |
| Response DTO | `{Resource}Dto` | `BookDto`, `AuthorDto` |
| Query object | `Get{Resource}sQuery` | `GetBooksQuery` |
| JSON fields | **camelCase** | `createdAt`, `totalCount` |
