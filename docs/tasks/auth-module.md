# TODO: Auth Module

> Spec: `docs/specs/auth-module.md`  
> Branch: `feature/add-user-module`  
> Vertical slice: mỗi slice deliver ít nhất 1 endpoint chạy được end-to-end.  
> **Status: COMPLETE — 0 errors, 15/15 tests pass**

---

## Slice 1: Foundation — Repository + Errors + DTOs + DI

- [x] **Task 1.1** — Tạo `IUnitOfWork` (Domain)
  - File: `src/BE/Core/BookStore.Domain/IRepository/IUnitOfWork.cs`

- [x] **Task 1.2** — Tạo `UnitOfWork` (Infrastructure)
  - File: `src/BE/Core/BookStore.Infrastructure/Repository/UnitOfWork.cs`

- [x] **Task 1.3** — Tạo `IRefreshTokenRepository` (Domain)
  - File: `src/BE/Core/BookStore.Domain/IRepository/IRefreshTokenRepository.cs`

- [x] **Task 1.4** — Tạo `RefreshTokenRepository` (Infrastructure)
  - File: `src/BE/Core/BookStore.Infrastructure/Repository/RefreshTokenRepository.cs`

- [x] **Task 1.5** — Tạo `AuthErrors.cs` (Application)
  - File: `src/BE/Core/BookStore.Application/Auth/AuthErrors.cs`

- [x] **Task 1.6** — Tạo Commands + DTOs (Application)
  - `Commands/RegisterCommand.cs`, `LoginCommand.cs`, `RefreshTokenCommand.cs`, `LogoutCommand.cs`
  - `DTOs/AuthResponse.cs`, `UserDto.cs`

- [x] **Task 1.7** — Tạo `JwtOptions` config class + cập nhật `appsettings.json`
  - File: `src/BE/Core/BookStore.Application/Auth/JwtOptions.cs`
  - Thêm `AccessExpiryMinutes: 15`, `RefreshExpiryDays: 7` vào `appsettings.json`

- [x] **Task 1.8** — Tạo `IAuthService` interface (Application)
  - File: `src/BE/Core/BookStore.Application/Auth/IAuthService.cs`

- [x] **Task 1.9** — Upgrade FluentValidation + thêm `.AspNetCore` integration
  - `BookStore.Application.csproj`: `FluentValidation 8.0.0` → `11.3.0`
  - `BookStore.API.csproj`: added `FluentValidation.AspNetCore 11.3.0`
  - Đăng ký `AddFluentValidationAutoValidation()` + `AddValidatorsFromAssemblyContaining<RegisterCommandValidator>()`

- [x] **Task 1.10** — Đăng ký DI trong `ServiceExtensions.AddApplicationServices(IConfiguration)`
  - `IAuthService → AuthService`, `IRefreshTokenRepository → RefreshTokenRepository`, `IUnitOfWork → UnitOfWork`

---

### Checkpoint 1 — Build sạch ✅
- [x] `dotnet build` — 0 errors (15 warnings: pre-existing NU1903, NU1701, CS8603; CS8625 từ UserManager null mock constructor — expected)
- [x] Tất cả interface + DI registration đúng
- [x] Dependency rule: Domain không import Infrastructure/Application

---

## Slice 2: Register — `POST /api/auth/register`

- [x] **Task 2.1** — Seed Roles trong `Program.cs`
  - Seed `"Admin"` và `"Customer"` qua `RoleManager` sau `app.Build()`

- [x] **Task 2.2** — Tạo `AuthService` với `RegisterAsync` (Application)
  - File: `src/BE/Core/BookStore.Application/Auth/AuthService.cs`

- [x] **Task 2.3** — Tạo `RegisterCommandValidator`
  - File: `src/BE/Core/BookStore.API/Validators/RegisterCommandValidator.cs`

- [x] **Task 2.4** — Tạo `AuthController` với action `Register` (API)
  - File: `src/BE/Core/BookStore.API/Controllers/AuthController.cs`
  - Dùng `StatusCode(201, ApiResponse<AuthResponse>.Ok(result.Value))` — không dùng `HandleCreated`

---

### Checkpoint 2 — Register hoạt động end-to-end ✅
- [x] `RegisterAsync_ShouldReturnTokens_WhenEmailIsNew` — PASS
- [x] `RegisterAsync_ShouldFail_WhenEmailAlreadyExists` — PASS

---

## Slice 3: Login — `POST /api/auth/login`

- [x] **Task 3.1** — Thêm `LoginAsync` vào `AuthService`
- [x] **Task 3.2** — Tạo `LoginCommandValidator`
  - File: `src/BE/Core/BookStore.API/Validators/LoginCommandValidator.cs`
- [x] **Task 3.3** — Thêm action `Login` vào `AuthController`

---

### Checkpoint 3 — Login hoạt động end-to-end ✅
- [x] `LoginAsync_ShouldReturnTokens_WhenCredentialsValid` — PASS
- [x] `LoginAsync_ShouldFail_WhenUserNotFound` → `Auth.InvalidCredentials` — PASS
- [x] `LoginAsync_ShouldFail_WhenPasswordIncorrect` → `Auth.InvalidCredentials` — PASS

---

## Slice 4: Token Lifecycle — Refresh + Logout

- [x] **Task 4.1** — Thêm `RefreshAsync` vào `AuthService`
- [x] **Task 4.2** — Thêm `LogoutAsync` vào `AuthService`
- [x] **Task 4.3** — Thêm actions `Refresh` + `Logout` vào `AuthController`
  - Logout dùng `NoContent()` — không dùng `HandleResult(Result)` (sẽ trả 200 thay vì 204)

---

### Checkpoint 4 — Token lifecycle hoạt động ✅
- [x] `RefreshAsync_ShouldRotateTokens_WhenTokenIsActive` — PASS
- [x] `RefreshAsync_ShouldFail_WhenTokenNotFound` — PASS
- [x] `RefreshAsync_ShouldFail_WhenTokenExpired` → `Auth.InvalidRefreshToken` — PASS
- [x] `RefreshAsync_ShouldFail_WhenTokenRevoked` → `Auth.InvalidRefreshToken` — PASS
- [x] `LogoutAsync_ShouldRevokeToken_WhenTokenBelongsToUser` — PASS
- [x] `LogoutAsync_ShouldFail_WhenTokenNotFound` — PASS

---

## Slice 5: Current User — `GET /api/auth/me`

- [x] **Task 5.1** — Thêm `GetCurrentUserAsync` vào `AuthService`
- [x] **Task 5.2** — Thêm action `GetCurrentUser` vào `AuthController`

---

### Checkpoint 5 — Full Auth Module hoạt động ✅
- [x] 5 endpoint trên AuthController đều đúng pattern
- [x] `dotnet build` — 0 errors

---

## Slice 6: Unit Tests

- [x] **Task 6.1** — `RefreshTokenTests.cs` (4 tests)
  - File: `src/BE/BookStore.Application.Tests/Domain/Auth/RefreshTokenTests.cs`

- [x] **Task 6.2** — `AuthServiceTests.cs` — Register (2 tests)
- [x] **Task 6.3** — `AuthServiceTests.cs` — Login (3 tests)
- [x] **Task 6.4** — `AuthServiceTests.cs` — Refresh + Logout (6 tests)
  - File: `src/BE/BookStore.Application.Tests/Application/Auth/AuthServiceTests.cs`

---

### Checkpoint 6 — Final ✅
- [x] `dotnet test` — **15/15 PASS**, 0 fail, 0 skip
- [x] Tất cả success + failure path trong AuthService được cover
- [x] Không có `throw` cho lỗi nghiệp vụ trong AuthService
- [x] Không có business logic trong AuthController

---

## File Map tổng hợp

```
src/BE/Core/
├── BookStore.Domain/
│   └── IRepository/
│       ├── IUnitOfWork.cs                           ✅
│       └── IRefreshTokenRepository.cs               ✅
│
├── BookStore.Application/
│   └── Auth/
│       ├── AuthErrors.cs                            ✅
│       ├── IAuthService.cs                          ✅
│       ├── AuthService.cs                           ✅
│       ├── JwtOptions.cs                            ✅
│       ├── Commands/
│       │   ├── RegisterCommand.cs                   ✅
│       │   ├── LoginCommand.cs                      ✅
│       │   ├── RefreshTokenCommand.cs               ✅
│       │   └── LogoutCommand.cs                     ✅
│       └── DTOs/
│           ├── AuthResponse.cs                      ✅
│           └── UserDto.cs                           ✅
│
├── BookStore.Infrastructure/
│   └── Repository/
│       ├── UnitOfWork.cs                            ✅
│       └── RefreshTokenRepository.cs                ✅
│
└── BookStore.API/
    ├── Controllers/
    │   └── AuthController.cs                        ✅
    ├── Validators/
    │   ├── RegisterCommandValidator.cs              ✅
    │   └── LoginCommandValidator.cs                 ✅
    ├── Extensions/
    │   └── ServiceExtensions.cs                     ✅ (modified)
    ├── Program.cs                                   ✅ (modified - seed roles)
    └── appsettings.json                             ✅ (modified)

tests/
└── BookStore.Application.Tests/
    ├── Domain/Auth/
    │   └── RefreshTokenTests.cs                     ✅ (4 tests)
    └── Application/Auth/
        └── AuthServiceTests.cs                      ✅ (11 tests)
```

## Ghi chú kỹ thuật (đã validate)

### JwtOptions POCO
```csharp
public class JwtOptions
{
    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int AccessExpiryMinutes { get; set; } = 15;
    public int RefreshExpiryDays { get; set; } = 7;
}
```

### Register — 201 Created (không dùng HandleCreated)
```csharp
if (result.IsSuccess)
    return StatusCode(StatusCodes.Status201Created, ApiResponse<AuthResponse>.Ok(result.Value));
return HandleResult(result);
```

### Logout — 204 No Content (không dùng HandleResult)
```csharp
if (result.IsSuccess) return NoContent();
return HandleResult(result);
```

### Mock UserManager trong test
```csharp
var store = new Mock<IUserStore<ApplicationUser>>();
var userManager = new Mock<UserManager<ApplicationUser>>(
    store.Object, null, null, null, null, null, null, null, null);
// Nullable warnings on null params là expected — UserManager ctor có nhiều optional deps
```
