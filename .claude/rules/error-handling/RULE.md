# Error Handling — BookStore (.NET)

## Core Principles
- **Không throw exception** cho lỗi nghiệp vụ — dùng Result Pattern
- Chỉ throw exception cho lỗi hệ thống không mong đợi (infrastructure, bug)
- Centralized exception handler qua Middleware
- Response lỗi luôn bọc trong `ApiResponse<T>`

## Error Taxonomy

| Loại lỗi | Xử lý | Ví dụ |
|----------|-------|-------|
| Nghiệp vụ (operational) | `Result.Failure(error)` | NotFound, Conflict, Unauthorized |
| Validation input | `ValidationApiResponse` (HTTP 400) | FluentValidation fail |
| Hệ thống (programmer) | throw → Middleware catch | DB connection, NullRef bug |

## Result Pattern — lỗi nghiệp vụ

```csharp
// Shared/Results/ErrorType.cs
public enum ErrorType { None, Failure, Validation, NotFound, Conflict, Unauthorized }

// Shared/Results/Error.cs
public sealed class Error
{
    public string Code        { get; }
    public string Description { get; }
    public ErrorType Type     { get; }

    public static Error NotFound    (string code, string desc) => new(code, desc, ErrorType.NotFound);
    public static Error Conflict    (string code, string desc) => new(code, desc, ErrorType.Conflict);
    public static Error Validation  (string code, string desc) => new(code, desc, ErrorType.Validation);
    public static Error Unauthorized(string code, string desc) => new(code, desc, ErrorType.Unauthorized);
    public static Error Failure     (string code, string desc) => new(code, desc, ErrorType.Failure);
}
```

### Định nghĩa lỗi theo module
```csharp
// Application/Books/BookErrors.cs — MỖI module tự quản lý lỗi
public static class BookErrors
{
    public static Error NotFound(Guid id)
        => Error.NotFound("Book.NotFound", $"Book '{id}' not found.");

    public static readonly Error TitleExists
        = Error.Conflict("Book.TitleExists", "A book with this title already exists.");
}
```

### Service — trả về Result, không throw
```csharp
// Đúng
public async Task<Result<BookDto>> GetByIdAsync(Guid id, CancellationToken ct)
{
    var book = await _bookRepo.GetByIdAsync(id, ct);
    if (book is null) return BookErrors.NotFound(id);  // implicit conversion
    return book.ToDto();                                // implicit conversion
}

// Sai
throw new NotFoundException($"Book {id} not found");
```

### Controller — ToActionResult() duy nhất
```csharp
// ResultExtensions tự map ErrorType → HTTP status
public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    => (await _bookService.GetByIdAsync(id, ct)).ToActionResult();
```

| `ErrorType` | HTTP Status |
|-------------|-------------|
| `NotFound` | 404 |
| `Conflict` | 409 |
| `Validation` | 400 |
| `Unauthorized` | 401 |
| `Failure` | 400 |

## Validation Errors — FluentValidation + ValidationFilter

```csharp
// API/Filters/ValidationFilter.cs — dùng chung toàn API
public class ValidationFilter<T> : IEndpointFilter where T : class
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext ctx, EndpointFilterDelegate next)
    {
        var validator = ctx.HttpContext.RequestServices.GetService<IValidator<T>>();
        if (validator is not null)
        {
            var model = ctx.Arguments.OfType<T>().FirstOrDefault();
            var result = await validator.ValidateAsync(model!);
            if (!result.IsValid)
                return Results.BadRequest(ValidationApiResponse.Fail(result.Errors));
        }
        return await next(ctx);
    }
}
```

```json
// Response HTTP 400
{
  "success": false,
  "message": "Validation failed.",
  "errors": {
    "Title": ["Title is required."],
    "Price": ["Price must be greater than zero."]
  }
}
```

## Global Exception Middleware — lỗi hệ thống

```csharp
// API/Middleware/ExceptionHandlingMiddleware.cs
public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext ctx)
    {
        try { await next(ctx); }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception: {Method} {Path}", ctx.Request.Method, ctx.Request.Path);
            ctx.Response.StatusCode = 500;
            await ctx.Response.WriteAsJsonAsync(ApiResponse<object>.Fail("An unexpected error occurred.", "Internal.Error"));
        }
    }
}

// Program.cs — đăng ký đầu tiên trong pipeline
app.UseMiddleware<ExceptionHandlingMiddleware>();
```

## Quy tắc bắt buộc
1. **Service layer** — chỉ dùng `Result.Failure(error)`, không throw
2. **Domain layer** — Entity method trả `Result`, dùng private ctor + factory
3. **Controller** — luôn kết thúc bằng `result.ToActionResult()`
4. **Không catch Exception** trong Service để "nuốt" lỗi im lặng
5. **Không log ở Service** — log tập trung tại Middleware
