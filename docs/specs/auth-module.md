# Feature: Auth Module

## Objective
Implement authentication và authorization cho BookStore API: đăng ký/đăng nhập bằng email+password, phát hành JWT Access Token + Refresh Token có thể revoke, phân quyền Admin/Customer qua Claims.

## Module
Auth

## Core Features

### 1. Register — Đăng ký tài khoản Customer
**Endpoint:** `POST /api/auth/register`  
**Acceptance:**
- Nhận `email`, `password`, `fullName`
- Validate format email, password ≥ 8 ký tự, fullName required
- Email phải unique (check qua Identity)
- Hash password bằng BCrypt thông qua ASP.NET Core Identity (`UserManager`)
- Tạo user với Role `Customer`
- Trả về `AuthResponse` gồm `accessToken`, `refreshToken`, `expiresAt`, `user` info

### 2. Login — Đăng nhập
**Endpoint:** `POST /api/auth/login`  
**Acceptance:**
- Nhận `email`, `password`
- Verify password bằng `UserManager.CheckPasswordAsync` (BCrypt internally)
- Fail nếu user không tồn tại hoặc sai password → `Auth.InvalidCredentials` (không phân biệt để tránh user enumeration)
- Phát hành cặp Access Token + Refresh Token mới
- Lưu Refresh Token vào DB (`RefreshTokens` table)
- Trả về `AuthResponse`

### 3. Refresh Token — Gia hạn Access Token
**Endpoint:** `POST /api/auth/refresh`  
**Acceptance:**
- Nhận `refreshToken` (string)
- Tìm token trong DB, kiểm tra `IsActive` (not expired, not revoked)
- Revoke token cũ (`token.Revoke()`)
- Phát hành cặp token mới (rotation)
- Trả về `AuthResponse` mới

### 4. Logout — Revoke Refresh Token
**Endpoint:** `POST /api/auth/logout`  
**Auth:** `[Authorize]` (any role)  
**Acceptance:**
- Nhận `refreshToken` từ body
- Tìm token thuộc user hiện tại (lấy userId từ Claims)
- Revoke token (`token.Revoke()`)
- Trả về 204 No Content

### 5. Get Current User — Thông tin user đang login
**Endpoint:** `GET /api/auth/me`  
**Auth:** `[Authorize]` (any role)  
**Acceptance:**
- Lấy userId từ JWT Claims (`NameIdentifier`)
- Trả về `UserDto` (id, email, fullName, role, avatarUrl)

## Out of Scope
- Forgot password / Reset password
- Email verification
- OAuth2 / Social login (Google, Facebook)
- Multi-factor authentication (MFA)
- Admin tạo tài khoản Admin mới (seed qua migration)
- Avatar upload (thuộc User Profile feature)
- Rate limiting / brute force protection

## Technical Approach

### Domain
**Entities đã có (không thay đổi):**
- `ApplicationUser` — `IdentityUser<Guid>` + FullName, AvatarUrl
- `RefreshToken` — Token, ExpiresAt, IsRevoked, RevokedAt, UserId
  - `IsActive` = `!IsRevoked && !IsExpired`
  - `Revoke()` method

**Không cần thêm entity hay migration** — schema đã có từ `InitialCreate`.

### Application Layer
Tạo mới hoàn toàn trong `BookStore.Application/`:

**Structure:**
```
Application/
  Auth/
    AuthErrors.cs
    IAuthService.cs
    AuthService.cs
    Commands/
      RegisterCommand.cs
      LoginCommand.cs
      RefreshTokenCommand.cs
      LogoutCommand.cs
    DTOs/
      AuthResponse.cs
      UserDto.cs
```

**IAuthService interface:**
```csharp
Task<Result<AuthResponse>> RegisterAsync(RegisterCommand cmd, CancellationToken ct = default);
Task<Result<AuthResponse>> LoginAsync(LoginCommand cmd, CancellationToken ct = default);
Task<Result<AuthResponse>> RefreshAsync(RefreshTokenCommand cmd, CancellationToken ct = default);
Task<Result>               LogoutAsync(LogoutCommand cmd, CancellationToken ct = default);
Task<Result<UserDto>>      GetCurrentUserAsync(Guid userId, CancellationToken ct = default);
```

**AuthErrors.cs:**
```csharp
public static class AuthErrors
{
    public static readonly Error InvalidCredentials =
        Error.Unauthorized("Auth.InvalidCredentials", "Email or password is incorrect.");
    public static readonly Error InvalidRefreshToken =
        Error.Unauthorized("Auth.InvalidRefreshToken", "Refresh token is invalid or expired.");
    public static readonly Error RefreshTokenNotFound =
        Error.NotFound("Auth.RefreshTokenNotFound", "Refresh token not found.");
    public static readonly Error UserNotFound =
        Error.NotFound("Auth.UserNotFound", "User not found.");
    public static readonly Error EmailAlreadyExists =
        Error.Conflict("Auth.EmailAlreadyExists", "An account with this email already exists.");
    public static readonly Error RegistrationFailed =
        Error.Failure("Auth.RegistrationFailed", "Failed to create account.");
}
```

**JWT Token Generation (trong AuthService):**
- Access Token: HS256, claims = `sub` (userId), `email`, `name`, `role`, `jti`
- Expiry: 15 phút (từ `JwtSettings:AccessExpiryMinutes`)
- Refresh Token: `Convert.ToBase64String(RandomNumberGenerator.GetBytes(64))`
- Refresh Expiry: 7 ngày (từ `JwtSettings:RefreshExpiryDays`)

### Infrastructure Layer
Tạo trong `BookStore.Infrastructure/`:

**IRefreshTokenRepository** (interface đặt trong `BookStore.Domain/IRepository/`):
```csharp
Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken ct = default);
Task<List<RefreshToken>> GetActiveByUserIdAsync(Guid userId, CancellationToken ct = default);
void Add(RefreshToken token);
```

**RefreshTokenRepository** (implementation trong Infrastructure):
- Dùng `AppDbContext` trực tiếp
- `SaveChangesAsync` gọi ở Service (Unit of Work pattern)

**IUnitOfWork** (interface trong Domain):
```csharp
Task<int> SaveChangesAsync(CancellationToken ct = default);
```

**UnitOfWork** (implementation trong Infrastructure):
- Wraps `AppDbContext.SaveChangesAsync`

### API Layer
**AuthController** kế thừa `BaseController`:
```
POST /api/auth/register   [AllowAnonymous]
POST /api/auth/login      [AllowAnonymous]
POST /api/auth/refresh    [AllowAnonymous]
POST /api/auth/logout     [Authorize]
GET  /api/auth/me         [Authorize]
```

**Validators (FluentValidation):**
- `RegisterCommandValidator`: Email format, Password length ≥ 8, FullName required max 100
- `LoginCommandValidator`: Email format, Password required

**DI Registration** (ServiceExtensions.cs):
```csharp
services.AddScoped<IAuthService, AuthService>();
services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
services.AddScoped<IUnitOfWork, UnitOfWork>();
```

**Roles Seeding** — trong `Program.cs` hoặc migration:
```csharp
// Seed Roles: "Admin", "Customer"
await roleManager.CreateAsync(new IdentityRole<Guid>("Admin"));
await roleManager.CreateAsync(new IdentityRole<Guid>("Customer"));
```

## Validation Plan

| Rule | Tầng | Công cụ |
|------|------|---------|
| Email format required | API | FluentValidation |
| Password ≥ 8 ký tự | API | FluentValidation |
| FullName required, max 100 | API | FluentValidation |
| Email unique | Application | `UserManager.FindByEmailAsync` |
| Credentials valid | Application | `UserManager.CheckPasswordAsync` |
| Refresh token active | Application | `token.IsActive` check |
| Refresh token belongs to user | Application | UserId match |

## DTOs

```csharp
// Commands (input)
public record RegisterCommand(string Email, string Password, string FullName);
public record LoginCommand(string Email, string Password);
public record RefreshTokenCommand(string RefreshToken);
public record LogoutCommand(Guid UserId, string RefreshToken);

// Responses (output)
public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    UserDto User
);

public record UserDto(
    Guid Id,
    string Email,
    string FullName,
    string Role,
    string? AvatarUrl
);
```

## Testing Strategy

### Unit Tests — AuthService
**Success paths:**
- `RegisterAsync_ShouldReturnTokens_WhenEmailIsNew`
- `LoginAsync_ShouldReturnTokens_WhenCredentialsValid`
- `RefreshAsync_ShouldRotateTokens_WhenRefreshTokenIsActive`
- `LogoutAsync_ShouldRevokeToken_WhenTokenBelongsToUser`

**Failure paths:**
- `RegisterAsync_ShouldFail_WhenEmailAlreadyExists`
- `LoginAsync_ShouldFail_WhenPasswordIncorrect` → `Auth.InvalidCredentials`
- `LoginAsync_ShouldFail_WhenUserNotFound` → `Auth.InvalidCredentials` (same error, no enumeration)
- `RefreshAsync_ShouldFail_WhenTokenExpired` → `Auth.InvalidRefreshToken`
- `RefreshAsync_ShouldFail_WhenTokenRevoked` → `Auth.InvalidRefreshToken`
- `LogoutAsync_ShouldFail_WhenTokenNotFound`

### Unit Tests — Domain (RefreshToken)
- `Revoke_ShouldSetIsRevokedTrue`
- `IsActive_ShouldReturnFalse_WhenExpired`
- `IsActive_ShouldReturnFalse_WhenRevoked`

**Mocks cần:** `UserManager<ApplicationUser>`, `IRefreshTokenRepository`, `IUnitOfWork`

## Sequence Flow

### Register
```
POST /api/auth/register
  → ValidationFilter (FluentValidation)
  → AuthController.Register
  → AuthService.RegisterAsync
      → UserManager.FindByEmailAsync → EmailAlreadyExists?
      → UserManager.CreateAsync (BCrypt hash internally)
      → UserManager.AddToRoleAsync("Customer")
      → GenerateAccessToken (JWT)
      → GenerateRefreshToken (random bytes)
      → RefreshTokenRepo.Add(refreshToken)
      → UnitOfWork.SaveChangesAsync
  ← AuthResponse { accessToken, refreshToken, expiresAt, user }
  ← 201 Created
```

### Login
```
POST /api/auth/login
  → ValidationFilter
  → AuthController.Login
  → AuthService.LoginAsync
      → UserManager.FindByEmailAsync → null? → InvalidCredentials
      → UserManager.CheckPasswordAsync → false? → InvalidCredentials
      → UserManager.GetRolesAsync
      → GenerateAccessToken + GenerateRefreshToken
      → RefreshTokenRepo.Add
      → UnitOfWork.SaveChangesAsync
  ← AuthResponse
  ← 200 OK
```

### Refresh
```
POST /api/auth/refresh
  → AuthController.Refresh
  → AuthService.RefreshAsync
      → RefreshTokenRepo.GetByTokenAsync → null? → RefreshTokenNotFound
      → token.IsActive? → false? → InvalidRefreshToken
      → token.Revoke() [mark old token revoked]
      → GenerateAccessToken + GenerateRefreshToken
      → RefreshTokenRepo.Add (new token)
      → UnitOfWork.SaveChangesAsync
  ← AuthResponse (new tokens)
  ← 200 OK
```

## Boundaries

### Always Do
- Dùng Result Pattern — không throw exception cho lỗi nghiệp vụ
- `Auth.InvalidCredentials` cho cả sai email lẫn sai password (tránh user enumeration)
- Refresh token rotation — revoke old, issue new
- Controller chỉ gọi `result.ToActionResult()`
- Validation đúng tầng (format ở API, business rule ở Application)

### Ask First
- Thêm seed Admin account (email + password cụ thể)
- Thêm `revoke-all` endpoint (logout tất cả devices)
- Cleanup job cho expired refresh tokens

### Never Do
- Trả về `UserNotFound` thay vì `InvalidCredentials` khi login (user enumeration)
- Lưu plain text password
- Log password hay token trong middleware
- Business logic trong Controller
- Domain Entity phụ thuộc Infrastructure
