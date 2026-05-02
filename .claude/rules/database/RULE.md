# Database Rules — BookStore (EF Core + SQL Server)

## General Rules
- Không viết raw SQL trong business logic — dùng EF Core LINQ
- Raw SQL (`FromSqlRaw`, `ExecuteSqlRaw`) chỉ dùng cho report/bulk khi LINQ không đủ
- Dùng **parameterized queries** — không string concatenation
- Multi-step write → dùng transaction
- Không log data nhạy cảm (password hash, token)

## DbContext — Connection Management
```csharp
// DI tự quản lý lifetime — đăng ký Scoped (mặc định)
builder.Services.AddDbContext<BookStoreDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

// Không new DbContext() thủ công trong service
```

## Query Best Practices

```csharp
// Select đúng field cần thiết — tránh load toàn bộ entity
var dto = await _context.Books
    .Where(b => b.Id == id && !b.IsDeleted)
    .Select(b => new BookDto { Id = b.Id, Title = b.Title, Price = b.Price })
    .FirstOrDefaultAsync(ct);

// Eager loading rõ ràng — không dùng lazy loading
var book = await _context.Books
    .Include(b => b.Category)
    .Include(b => b.Authors)
    .FirstOrDefaultAsync(b => b.Id == id, ct);

// Paging dùng QueryableExtensions (Shared)
return await query
    .ApplySort(request.SortBy, request.IsAscending)
    .ToPagedResultAsync(request, ct);

// Không load toàn bộ rồi filter in-memory
var books = await _context.Books.ToListAsync();
var filtered = books.Where(b => b.Price > 100); // N+1 risk
```

## Transactions
```csharp
// Dùng transaction cho multi-step write
await using var tx = await _context.Database.BeginTransactionAsync(ct);
try
{
    var order = new Order(...);
    _context.Orders.Add(order);

    foreach (var item in order.Items)
        await _inventoryRepo.DecrementStockAsync(item.BookId, item.Quantity, ct);

    await _context.SaveChangesAsync(ct);
    await tx.CommitAsync(ct);
    return order;
}
catch
{
    await tx.RollbackAsync(ct);
    throw;
}
```

## EF Core Configuration — Fluent API
```csharp
// Toàn bộ config trong EntityTypeConfiguration, không dùng Data Annotation
public class BookConfiguration : IEntityTypeConfiguration<Book>
{
    public void Configure(EntityTypeBuilder<Book> builder)
    {
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Title).IsRequired().HasMaxLength(300);
        builder.Property(b => b.Price).HasPrecision(18, 2);
        builder.HasQueryFilter(b => !b.IsDeleted); // Soft delete global filter
        builder.HasIndex(b => b.Title).IsUnique();
    }
}
```

## Migrations
- Dùng EF Core Migrations — không sửa schema DB trực tiếp
- File migration là immutable sau khi apply
- Đặt tên migration mô tả rõ: `AddBookSoftDelete`, `CreateOrderTable`

```bash
dotnet ef migrations add <MigrationName> --project BookStore.Infrastructure --startup-project BookStore.API
dotnet ef database update --project BookStore.Infrastructure --startup-project BookStore.API
```

## Naming Conventions (SQL Server)
| Thành phần | Convention | Ví dụ |
|------------|-----------|-------|
| Table | PascalCase số ít | `Book`, `Order`, `Author` |
| Column | PascalCase | `CreatedAt`, `IsDeleted` |
| PK | `Id` | `Id` (Guid) |
| FK | `{Entity}Id` | `CategoryId`, `UserId` |
| Index | `IX_{Table}_{Column}` | `IX_Books_Title` |
| Unique index | `UX_{Table}_{Column}` | `UX_Books_Title` |

## N+1 Prevention
```csharp
// N+1 — truy vấn Authors trong vòng lặp
foreach (var book in books)
    book.Authors = await _context.Authors.Where(a => a.BookId == book.Id).ToListAsync();

// Include một lần
var books = await _context.Books.Include(b => b.Authors).ToListAsync(ct);
```

## Soft Delete
```csharp
// Book có IsDeleted — dùng Global Query Filter
builder.HasQueryFilter(b => !b.IsDeleted);

// Xóa mềm trong repository
public async Task DeleteAsync(Guid id, CancellationToken ct)
{
    var book = await _context.Books.FindAsync([id], ct);
    if (book is null) return;
    book.IsDeleted = true;
    await _context.SaveChangesAsync(ct);
}
```

## Repository Pattern
```csharp
// Interface trong Application layer
public interface IBookRepository
{
    Task<Book?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<bool> ExistsByTitleAsync(string title, CancellationToken ct = default);
    void Add(Book book);
    void Remove(Book book);
}

// SaveChangesAsync gọi ở Service, không trong Repository
public async Task<Result<Guid>> CreateAsync(CreateBookCommand cmd, CancellationToken ct)
{
    var book = Book.Create(cmd.Title, cmd.Price);
    _bookRepo.Add(book);
    await _unitOfWork.SaveChangesAsync(ct);
    return book.Id;
}
```
