# BookStore API

A RESTful e-commerce backend built with ASP.NET Core 9 and Entity Framework Core.

## Architecture

```
BookStore.API          → Presentation (Controllers, Middleware)
BookStore.Application  → Business Logic (Services, DTOs, Interfaces)
BookStore.Domain       → Domain Models (Entities, Repository Interfaces)
BookStore.Infrastructure → Data Access (EF Core, MinIO, Migrations)
BookStore.Shared       → Cross-cutting (Result pattern, Error types)
```

## Features

- **Authentication** — JWT access tokens + refresh token rotation, BCrypt password hashing (work factor 12), role-based authorization (Admin / Customer)
- **Catalog** — Books, Authors, Categories (hierarchical), Publishers, Book images (MinIO), Book files
- **Shopping** — Cart management, Checkout flow, Order lifecycle (Pending → Paid → Shipped → Completed / Cancelled)
- **Payments** — Sandbox payment simulation, payment status tracking, refund requests
- **Pricing** — Dynamic price history, percentage discounts, coupons
- **Inventory** — Stock management, warehouse tracking, inventory transactions
- **User Management** — User profiles, multiple delivery addresses, device tracking

## Tech Stack

| Layer | Technology |
|---|---|
| Framework | ASP.NET Core 9 |
| ORM | Entity Framework Core 9 |
| Database | SQL Server |
| File Storage | MinIO (S3-compatible) |
| Authentication | JWT Bearer + Refresh Token Rotation |
| Password Hashing | BCrypt (work factor 12) |
| Testing | xUnit + Moq + FluentAssertions |
| API Docs | Swagger / OpenAPI |

## Design Patterns

- **Result Pattern** — All service methods return `BaseResult<T>` instead of throwing exceptions, with explicit `Map`, `Bind`, `Match` operators
- **Repository + Unit of Work** — All data access abstracted behind interfaces; testable without EF Core
- **Layered Architecture** — Strict dependency rule: outer layers depend on inner layers, never the reverse

## Security Notes

> **Password hashing:** Existing users registered before BCrypt migration will need to re-register. SHA256 hashes are not compatible with BCrypt.

## Getting Started

### Prerequisites

- .NET 9 SDK
- SQL Server (or Docker)
- MinIO (or Docker)

### Run with Docker Compose

```bash
docker-compose -f docker-compose.dev.yml up
```

### Run locally

```bash
# 1. Set connection string in appsettings.Development.json
# 2. Run migrations (auto-applied on startup in Development)
dotnet run --project Core/BookStore.API
```

API available at: `http://localhost:5000`  
Swagger UI: `http://localhost:5000/swagger`

## Running Tests

```bash
dotnet test BookStore.Application.Tests
```

Test coverage includes:
- Auth flow: login, logout, token refresh, password reset
- Book service: create, read (paginated), update, delete
- User profile and address management
