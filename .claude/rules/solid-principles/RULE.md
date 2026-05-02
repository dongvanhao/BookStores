# SOLID Principles — BookStore (.NET)

Mọi code được generate PHẢI tuân thủ 5 nguyên tắc SOLID. Vi phạm bất kỳ nguyên tắc nào phải được chỉ ra trước khi hoàn thành task.

---

## S — Single Responsibility Principle

**Mỗi class chỉ có một lý do để thay đổi.**

```csharp
// ❌ Sai — BookService vừa business logic, vừa gửi email, vừa log
public class BookService
{
    public async Task<Result<Guid>> CreateAsync(CreateBookCommand cmd)
    {
        // business logic
        _logger.LogInformation("Book created");           // logging — không phải trách nhiệm của Service
        await _emailSender.SendNewBookEmailAsync(cmd);    // side-effect — không phải trách nhiệm của Service
    }
}

// ✅ Đúng — mỗi class một trách nhiệm
public class BookService        { /* chỉ business logic */  }
public class BookEventPublisher { /* chỉ publish event */   }
// Logging tập trung tại Middleware
```

**Quy tắc áp dụng:**
- Service: chỉ chứa business logic, không log, không gửi email/notification trực tiếp
- Repository: chỉ data access, không có business rule
- Controller: chỉ nhận request → gọi service → trả response
- Validator: chỉ validate format/required, không query DB

---

## O — Open/Closed Principle

**Mở để mở rộng, đóng để sửa đổi.**

```csharp
// ❌ Sai — phải sửa switch khi thêm payment method mới
public decimal CalculateDiscount(string type)
{
    return type switch
    {
        "vip"      => 0.2m,
        "regular"  => 0.1m,
        "student"  => 0.15m,
        _          => 0m
    };
}

// ✅ Đúng — thêm loại mới không cần sửa code cũ
public interface IDiscountStrategy
{
    decimal Calculate(Order order);
}

public class VipDiscountStrategy     : IDiscountStrategy { ... }
public class StudentDiscountStrategy : IDiscountStrategy { ... }
// Thêm loại mới → tạo class mới, không sửa existing code
```

**Quy tắc áp dụng:**
- Dùng interface/abstract class thay vì switch-on-type trong domain logic
- Extension method cho Shared utilities (không sửa core class)
- Strategy pattern cho business rule có nhiều biến thể (pricing, discount, shipping)

---

## L — Liskov Substitution Principle

**Subtype phải thay thế được supertype mà không làm hỏng behavior.**

```csharp
// ❌ Sai — ReadOnlyRepository override Add() ném exception
public class ReadOnlyBookRepository : BookRepository
{
    public override void Add(Book book)
        => throw new NotSupportedException(); // vi phạm LSP
}

// ✅ Đúng — tách interface theo khả năng thực sự
public interface IReadOnlyBookRepository
{
    Task<Book?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<PagedResult<Book>> GetPagedAsync(GetBooksQuery q, CancellationToken ct);
}

public interface IBookRepository : IReadOnlyBookRepository
{
    void Add(Book book);
    void Remove(Book book);
}
```

**Quy tắc áp dụng:**
- Không override method để ném `NotSupportedException` hay `NotImplementedException`
- Interface Segregation (ISP) giải quyết hầu hết vi phạm LSP
- Domain entity: subclass không được bỏ invariant của base class

---

## I — Interface Segregation Principle

**Client không nên bị buộc phụ thuộc vào interface mà nó không dùng.**

```csharp
// ❌ Sai — một interface quá rộng
public interface IBookService
{
    Task<Result<BookDto>>         GetByIdAsync(Guid id, CancellationToken ct);
    Task<Result<PagedResult<BookDto>>> GetAllAsync(GetBooksQuery q, CancellationToken ct);
    Task<Result<Guid>>            CreateAsync(CreateBookCommand cmd, CancellationToken ct);
    Task<Result>                  UpdateAsync(UpdateBookCommand cmd, CancellationToken ct);
    Task<Result>                  DeleteAsync(Guid id, CancellationToken ct);
    Task<Result<string>>          UploadCoverAsync(Guid id, IFormFile file, CancellationToken ct);
    Task<Result>                  SyncInventoryAsync(CancellationToken ct);   // chỉ Admin dùng
    Task<Result<BookStatsDto>>    GetStatsAsync(CancellationToken ct);        // chỉ Dashboard dùng
}

// ✅ Đúng — tách theo consumer
public interface IBookQueryService
{
    Task<Result<BookDto>>              GetByIdAsync(Guid id, CancellationToken ct);
    Task<Result<PagedResult<BookDto>>> GetAllAsync(GetBooksQuery q, CancellationToken ct);
}

public interface IBookCommandService
{
    Task<Result<Guid>> CreateAsync(CreateBookCommand cmd, CancellationToken ct);
    Task<Result>       UpdateAsync(UpdateBookCommand cmd, CancellationToken ct);
    Task<Result>       DeleteAsync(Guid id, CancellationToken ct);
}

public interface IBookAdminService
{
    Task<Result<string>>       UploadCoverAsync(Guid id, IFormFile file, CancellationToken ct);
    Task<Result>               SyncInventoryAsync(CancellationToken ct);
    Task<Result<BookStatsDto>> GetStatsAsync(CancellationToken ct);
}
```

**Quy tắc áp dụng:**
- Interface service tối đa 5–6 method; nếu nhiều hơn → tách Query/Command
- Repository: tách `IReadOnlyRepository` nếu có consumer chỉ đọc
- Không inject interface "toàn năng" vào class chỉ dùng 1–2 method

---

## D — Dependency Inversion Principle

**High-level module không phụ thuộc low-level module — cả hai phụ thuộc abstraction.**

```csharp
// ❌ Sai — Service phụ thuộc trực tiếp vào concrete class
public class BookService
{
    private readonly BookRepository _repo;          // concrete, không phải interface
    private readonly SqlBookRepository _sqlRepo;    // tệ hơn: phụ thuộc infrastructure

    public BookService()
    {
        _repo = new BookRepository();               // new trực tiếp — không testable
    }
}

// ✅ Đúng — phụ thuộc abstraction, inject qua constructor
public class BookService
{
    private readonly IBookRepository _bookRepo;
    private readonly IUnitOfWork     _unitOfWork;

    public BookService(IBookRepository bookRepo, IUnitOfWork unitOfWork)
    {
        _bookRepo   = bookRepo;
        _unitOfWork = unitOfWork;
    }
}

// Registration trong Infrastructure (composition root)
services.AddScoped<IBookRepository, BookRepository>();
services.AddScoped<IBookService,    BookService>();
```

**Quy tắc áp dụng:**
- Application layer chỉ biết interface, không biết concrete class của Infrastructure
- Không `new` dependency bên trong class — luôn inject qua constructor
- Không dùng `ServiceLocator` / `IServiceProvider` trong Service
- Domain layer không inject bất cứ thứ gì — không có DI trong Entity

---

## Checklist SOLID khi generate code

Trước khi hoàn thành bất kỳ class/service/repository nào, xác nhận:

| Nguyên tắc | Câu hỏi kiểm tra |
|------------|-----------------|
| **SRP** | Class này có nhiều hơn 1 lý do để thay đổi không? |
| **OCP** | Thêm behavior mới có phải sửa code cũ không? |
| **LSP** | Subclass/implementation có override để ném exception không? |
| **ISP** | Interface có method nào mà consumer không dùng không? |
| **DIP** | Class có `new` dependency hoặc phụ thuộc concrete class không? |

Nếu bất kỳ câu trả lời nào là **có** → refactor trước khi hoàn thành.
