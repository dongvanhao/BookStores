---
name: security-review
description: Security audit cho BookStore ASP.NET Core — JWT, BCrypt, EF Core, MinIO
---

# Security Review Skill — BookStore (.NET)

## Purpose
Scan codebase tìm security vulnerability và produce prioritized report.

---

## Checklist

###  Critical (Check First)

- [ ] Secret hardcode trong source code
```bash
# Tìm connection string, secret, password trong code
grep -rn "password\s*=\s*['\"]" src/
grep -rn "SecretKey\s*=\s*['\"]" src/
grep -rn "AccessKey\s*=\s*['\"]" src/
```

- [ ] `.env` file bị commit lên git
```bash
git log --all --full-history -- .env
git log --all --full-history -- "*.env"
```

- [ ] Raw SQL với string concatenation (SQL Injection)
```csharp
//  Nguy hiểm
var query = $"SELECT * FROM Books WHERE Title = '{title}'";
_context.Database.ExecuteSqlRaw(query);

//  Đúng — EF Core parameterized
_context.Database.ExecuteSqlRaw("SELECT * FROM Books WHERE Title = {0}", title);
// Hoặc dùng LINQ — tự động parameterized
_context.Books.Where(b => b.Title == title);
```

- [ ] Password lưu plain text (không hash BCrypt)
```csharp
// 
user.PasswordHash = password;

// 
user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
```

###  High Priority

- [ ] Endpoint cần auth thiếu `[Authorize]`
```csharp
// Kiểm tra mọi Controller action
// Admin endpoint phải có [Authorize(Roles = "Admin")]
// Customer endpoint phải có [Authorize]
```

- [ ] JWT config yếu
```json
// appsettings.json — kiểm tra
{
  "JwtSettings": {
    "SecretKey": "???",         // phải >= 32 ký tự
    "AccessExpiryMinutes": 15,  // không nên > 60
    "RefreshExpiryDays": 7,     // không nên > 30
    "ValidateIssuer": true,     // phải true
    "ValidateAudience": true,   // phải true
    "ValidateLifetime": true    // phải true
  }
}
```

- [ ] Refresh token không thể revoke (không lưu DB)
- [ ] BCrypt work factor < 12
- [ ] Missing input validation — FluentValidation chưa đăng ký
- [ ] Authorization không check resource ownership
```csharp
//  User có thể cancel order của người khác
var order = await _orderRepo.GetByIdAsync(orderId, ct);

//  Verify ownership
var order = await _orderRepo.GetByIdAsync(orderId, ct);
if (order.UserId != currentUserId) return Result.Failure(OrderErrors.Forbidden);
```

###  Medium Priority

- [ ] Dependency có vulnerability đã biết
```bash
dotnet list package --vulnerable
dotnet list package --outdated
```

- [ ] Sensitive data trong log
```csharp
//  Log password hash hoặc token
_logger.LogInformation("User logged in: {Hash}", user.PasswordHash);

//  Chỉ log non-sensitive
_logger.LogInformation("User logged in: {UserId}", user.Id);
```

- [ ] MinIO presigned URL không có expiry
```csharp
//  Phải set expiry
var url = await _minioClient.PresignedGetObjectAsync(
    new PresignedGetObjectArgs()
        .WithBucket(bucket)
        .WithObject(objectName)
        .WithExpiry(3600)); // 1 giờ
```

- [ ] CORS quá rộng trong production
```csharp
//  Production
app.UseCors(b => b.AllowAnyOrigin());

app.UseCors(b => b.WithOrigins("https://yourdomain.com"));
```

- [ ] Error response trả stack trace ra client
```csharp
//  ExceptionMiddleware trả exception.ToString()
//  Chỉ trả generic message
ApiResponse<object>.Fail("An unexpected error occurred.", "Internal.Error")
```

### ℹ️ Low / Informational

- [ ] Missing security headers (X-Content-Type-Options, X-Frame-Options)
- [ ] Cookie không có HttpOnly / Secure flag (nếu dùng cookie)
- [ ] Docker image dùng root user thay vì non-root
- [ ] MinIO bucket public thay vì private + presigned URL

---

## Scan Commands

```bash
# Dependency vulnerability
dotnet list package --vulnerable --include-transitive

# Secret patterns trong C# files
grep -rn --include="*.cs" -E "(password|secret|apikey|connectionstring)\s*=\s*""[^""]{6,}" src/

# .env bị track
git ls-files | grep -E "\.env"

# appsettings chứa secret thật (không phải placeholder)
grep -rn --include="*.json" "SecretKey" src/
```

---

## Output Format

```markdown
# Security Review Report — [Date]

## Critical Issues
- `Infrastructure/Auth/JwtTokenService.cs:15` — SecretKey hardcode trong code
- `API/Controllers/OrdersController.cs:45` — thiếu [Authorize] trên DELETE endpoint

## High Priority
- `Application/Orders/OrderService.cs:78` — không verify ownership trước khi cancel

## Medium Priority
- `Infrastructure/Storage/MinioStorageService.cs:32` — presigned URL không set expiry

## Recommendations (theo priority)
1. Move tất cả secret vào .env, không commit .env
2. Thêm [Authorize] + ownership check cho Order endpoints
3. Set expiry cho MinIO presigned URL
```
