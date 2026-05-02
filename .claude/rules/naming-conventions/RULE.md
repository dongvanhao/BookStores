# Naming Conventions — BookStore (.NET)

## C# Code

| Thành phần | Convention | Ví dụ |
|------------|-----------|-------|
| Class, Interface, Record | PascalCase | `BookService`, `IBookRepository` |
| Method, Property | PascalCase | `GetByIdAsync`, `TotalCount` |
| Private field | `_camelCase` | `_bookRepository`, `_context` |
| Local variable, param | camelCase | `bookId`, `cancellationToken` |
| Constant | PascalCase | `MaxPageSize` |
| Enum | PascalCase (cả value) | `ErrorType.NotFound` |
| Generic type param | `T` hoặc `TEntity` | `Result<T>`, `PagedResult<TItem>` |

## File & Folder (.NET)

| Loại | Convention | Ví dụ |
|------|-----------|-------|
| File C# | PascalCase, match class | `BookService.cs`, `IBookRepository.cs` |
| Folder | PascalCase | `Books/`, `Authors/`, `Common/` |
| Test file | `{Class}Tests.cs` | `BookServiceTests.cs` |
| Error class | `{Module}Errors.cs` | `BookErrors.cs`, `OrderErrors.cs` |
| Validator | `{Command}Validator.cs` | `CreateBookCommandValidator.cs` |
| Config | `{Entity}Configuration.cs` | `BookConfiguration.cs` |

### Cấu trúc folder theo module (Application layer)
```
Application/
  Books/
    BookErrors.cs
    BookService.cs
    Commands/
      CreateBookCommand.cs
    Queries/
      GetBooksQuery.cs
    DTOs/
      BookDto.cs
      CreateBookRequest.cs
    Validators/
      CreateBookCommandValidator.cs
```

## Database (SQL Server + EF Core)

| Thành phần | Convention | Ví dụ |
|------------|-----------|-------|
| Table | PascalCase, số ít | `Book`, `Order`, `Author` |
| Column | PascalCase | `CreatedAt`, `IsDeleted` |
| PK | `Id` (Guid) | `Id` |
| FK | `{Entity}Id` | `CategoryId`, `UserId` |
| Junction table | `{A}{B}` | `BookAuthor` |
| Index | `IX_{Table}_{Column}` | `IX_Books_Title` |
| Unique index | `UX_{Table}_{Column}` | `UX_Books_Title` |
| Migration | PascalCase mô tả | `AddBookSoftDelete`, `CreateOrderTable` |

## URL / Route

```
# Plural nouns, kebab-case, không có version prefix
GET    /api/books
GET    /api/books/{id}
POST   /api/books
PUT    /api/books/{id}
DELETE /api/books/{id}

# Nested
GET    /api/orders/{id}/items
GET    /api/authors/{id}/books

# Auth actions (verb hợp lệ)
POST   /api/auth/login
POST   /api/auth/refresh
POST   /api/auth/logout

# Admin
GET    /api/dashboard/stats
```

## Environment Variables (.env / appsettings)

```bash
# .env — UPPER_SNAKE_CASE
ASPNETCORE_ENVIRONMENT=Development

# Database
CONNECTION_STRING_DEFAULT=Server=...;Database=BookStore;...

# JWT
JWT_SECRET_KEY=...
JWT_ISSUER=BookStoreAPI
JWT_AUDIENCE=BookStoreClient
JWT_ACCESS_EXPIRY_MINUTES=15
JWT_REFRESH_EXPIRY_DAYS=7

# MinIO
MINIO_ENDPOINT=localhost:9000
MINIO_ACCESS_KEY=...
MINIO_SECRET_KEY=...
MINIO_BUCKET_BOOKS=book-images
MINIO_BUCKET_AVATARS=author-avatars
MINIO_USE_SSL=false

# BCrypt
BCRYPT_WORK_FACTOR=12
```

```json
// appsettings.json — PascalCase keys
{
  "ConnectionStrings": { "Default": "..." },
  "JwtSettings": {
    "SecretKey": "...",
    "Issuer": "BookStoreAPI",
    "AccessExpiryMinutes": 15,
    "RefreshExpiryDays": 7
  },
  "MinioSettings": {
    "Endpoint": "localhost:9000",
    "AccessKey": "...",
    "SecretKey": "...",
    "BucketBooks": "book-images",
    "BucketAvatars": "author-avatars",
    "UseSsl": false
  }
}
```

## DTO & Request Naming

| Pattern | Ví dụ |
|---------|-------|
| `{Resource}Dto` | `BookDto`, `AuthorDto`, `OrderDto` |
| `Create{Resource}Request` | `CreateBookRequest` |
| `Update{Resource}Request` | `UpdateBookRequest` |
| `Get{Resource}sQuery` | `GetBooksQuery`, `GetOrdersQuery` |
| `{Resource}Response` | `AuthResponse`, `LoginResponse` |
| `PagedResult<{Resource}Dto>` | `PagedResult<BookDto>` |

## Error Code Naming

```
{Module}.{Action}        →  "Book.NotFound"
{Module}.{Constraint}    →  "Book.TitleExists"
{Module}.{State}         →  "Order.CannotCancel"
Auth.{Issue}             →  "Auth.InvalidCredentials"
```
