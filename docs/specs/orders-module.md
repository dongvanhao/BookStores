# Feature: Orders Module

## Objective
Build module Orders cho phép Customer tạo đơn hàng từ danh sách sản phẩm, xem lịch sử đơn hàng, và Admin/Customer thực hiện transition trạng thái theo state machine cố định.

## Module
**Orders** — liên quan: Books (stock), Auth (userId từ JWT)

---

## Core Features

### 1. Create Order
**Actor:** Customer (authenticated)  
**Acceptance:**
- Nhận list items `[{bookId, quantity}]` + `shippingAddress` + `note?`
- Validate mỗi Book tồn tại và đủ `StockQuantity`
- Tạo `Order` entity qua `Order.Create()`, thêm items qua `Order.AddItem()`
- Giảm `StockQuantity` trên từng `Book` (atomic — rollback nếu fail)
- Trả `OrderId` (201 Created)
- Order khởi tạo luôn ở trạng thái `Pending`

### 2. Get Order History (Customer)
**Actor:** Customer (authenticated)  
**Acceptance:**
- Trả danh sách đơn hàng của chính user đang đăng nhập (lấy `UserId` từ JWT)
- Hỗ trợ filter theo `status`, sort, pagination
- Mỗi item trong list hiển thị: `id`, `status`, `totalAmount`, `itemCount`, `createdAt`

### 3. Get Order Detail
**Actor:** Customer (own) hoặc Admin  
**Acceptance:**
- Trả chi tiết đơn hàng kèm `Items` (bookTitle, quantity, unitPrice, subTotal)
- Customer chỉ xem được đơn của chính mình — trả `404` nếu không phải
- Admin xem được tất cả

### 4. State Machine Transitions
**Actor:** Admin (confirm, ship, deliver) / Customer hoặc Admin (cancel)

| Endpoint | Actor | Transition | Domain Method |
|----------|-------|-----------|---------------|
| `PATCH /api/orders/{id}/confirm` | Admin | Pending → Confirmed | `order.Confirm()` |
| `PATCH /api/orders/{id}/ship` | Admin | Confirmed → Shipped | `order.Ship()` |
| `PATCH /api/orders/{id}/deliver` | Admin | Shipped → Delivered | `order.Deliver()` |
| `PATCH /api/orders/{id}/cancel` | Admin / Customer | Pending/Confirmed → Cancelled | `order.Cancel()` |

**Rule:** Service **không** tự kiểm tra trạng thái hợp lệ. Gọi domain method → nhận `Result` → propagate lên.  
Khi Cancel, restore `StockQuantity` cho tất cả items.

---

## Out of Scope
- Payment / checkout flow (thanh toán online)
- Cart entity riêng (cart là concept ở FE / request body, không lưu DB)
- Order tracking / notifications
- Admin xem all orders (GET /api/admin/orders — có thể thêm sau)
- Partial cancel (hủy một phần items)

---

## Technical Approach

### Domain (đã có — KHÔNG sửa)

| File | Trạng thái | Ghi chú |
|------|-----------|---------|
| `Domain/Entities/Order.cs` | ✅ Hoàn chỉnh | Factory + state machine đầy đủ |
| `Domain/Entities/OrderItem.cs` | ✅ Hoàn chỉnh | Snapshot `BookTitle`, computed `SubTotal` |
| `Domain/Enums/OrderStatus.cs` | ✅ Hoàn chỉnh | Pending/Confirmed/Shipped/Delivered/Cancelled |
| `Domain/Errors/OrderErrors.cs` | ✅ Hoàn chỉnh | NotFound, InvalidTransition, CannotCancel, Empty |
| `Infrastructure/Configurations/OrderConfiguration.cs` | ✅ Hoàn chỉnh | — |
| `Infrastructure/Configurations/OrderItemConfiguration.cs` | ✅ Hoàn chỉnh | — |
| `Infrastructure/Data/AppDbContext.cs` | ✅ `DbSet<Order>` + `DbSet<OrderItem>` | — |

**State Machine đã implement trong Domain:**
```
Pending  ──Confirm()──▶  Confirmed ──Ship()──▶ Shipped ──Deliver()──▶ Delivered
   │                        │
   └──────Cancel()──────────┘
   (Shipped/Delivered/Cancelled → Cancel() trả Failure)
```

### Infrastructure — Cần tạo

**`IOrderRepository`** (Domain/IRepository):
```csharp
public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Order?> GetByIdWithItemsAsync(Guid id, CancellationToken ct = default);
    IQueryable<Order> GetQueryable();
    void Add(Order order);
}
```

**`OrderRepository`** (Infrastructure/Repository):
- `GetByIdWithItemsAsync`: `.Include(o => o.Items).ThenInclude(i => i.Book)` — dùng cho detail + cancel (cần items để restore stock)
- `GetQueryable()`: trả `IQueryable<Order>` không Include — dùng cho paged list

### Application Layer — Cần tạo

#### Commands
```
Application/Orders/Commands/
  CreateOrderCommand.cs      — record với ShippingAddress, Note?, Items: IReadOnlyList<CreateOrderItemCommand>
  CreateOrderItemCommand.cs  — record với BookId, Quantity
```

#### Queries
```
Application/Orders/Queries/
  GetOrdersQuery.cs          — kế thừa QueryParams, thêm: OrderStatus? Status
```

#### DTOs
```
Application/Orders/DTOs/
  OrderDto.cs         — Id, Status (string), TotalAmount, ItemCount, CreatedAt
  OrderDetailDto.cs   — OrderDto + Items: IReadOnlyList<OrderItemDto>, ShippingAddress, Note
  OrderItemDto.cs     — BookId, BookTitle, Quantity, UnitPrice, SubTotal
```

#### IService Interfaces
```
Application/Orders/IService/
  IOrderCommandService.cs
  IOrderQueryService.cs
```

**`IOrderCommandService`:**
```csharp
Task<Result<Guid>>  CreateAsync(CreateOrderCommand cmd, Guid userId, CancellationToken ct = default);
Task<Result>        ConfirmAsync(Guid orderId, CancellationToken ct = default);
Task<Result>        ShipAsync(Guid orderId, CancellationToken ct = default);
Task<Result>        DeliverAsync(Guid orderId, CancellationToken ct = default);
Task<Result>        CancelAsync(Guid orderId, Guid requesterId, bool isAdmin, CancellationToken ct = default);
```

**`IOrderQueryService`:**
```csharp
Task<Result<PagedResult<OrderDto>>>  GetOrderHistoryAsync(GetOrdersQuery query, Guid userId, CancellationToken ct = default);
Task<Result<OrderDetailDto>>         GetByIdAsync(Guid orderId, Guid requesterId, bool isAdmin, CancellationToken ct = default);
```

#### Services Implementation

**`OrderCommandService`** — luồng `CreateAsync`:
1. Validate `cmd.Items` không rỗng → `OrderErrors.Empty`
2. Fetch tất cả Books theo `bookIds` — batch query (không N+1)
3. Với mỗi item: kiểm tra book tồn tại, `book.TryReduceStock(qty)` — nếu fail → `BookErrors.InsufficientStock`
4. `var order = Order.Create(userId, cmd.ShippingAddress, cmd.Note)`
5. Với mỗi item: `order.AddItem(book.Id, book.Title, item.Quantity, book.Price)` — domain method tự guard
6. `_orderRepo.Add(order)`
7. `await _unitOfWork.SaveChangesAsync(ct)` — atomic (stock + order trong 1 transaction)
8. `return order.Id`

**`OrderCommandService`** — luồng transition (Confirm/Ship/Deliver):
```csharp
public async Task<Result> ConfirmAsync(Guid orderId, CancellationToken ct)
{
    var order = await _orderRepo.GetByIdAsync(orderId, ct);
    if (order is null) return OrderErrors.NotFound(orderId);
    var result = order.Confirm();          // domain tự guard
    if (result.IsFailure) return result;
    await _unitOfWork.SaveChangesAsync(ct);
    return Result.Success();
}
```

**`OrderCommandService`** — luồng `CancelAsync`:
1. Lấy order `WithItems` (cần items để restore stock)
2. Kiểm tra ownership nếu không phải Admin
3. `order.Cancel()` — domain tự guard
4. Restore stock: `foreach item → book.RestoreStock(item.Quantity)`
5. `SaveChangesAsync`

**`OrderQueryService`** — luồng `GetOrderHistoryAsync`:
- `_orderRepo.GetQueryable().Where(o => o.UserId == userId)`
- Filter theo `query.Status` nếu có
- `.Select(o => new OrderDto { ..., ItemCount = o.Items.Count })`
- `.ApplySort(...)` + `.ToPagedResultAsync(...)`

**`OrderQueryService`** — luồng `GetByIdAsync`:
- Fetch `WithItems`
- Nếu không phải Admin: kiểm tra `order.UserId == requesterId` → `OrderErrors.NotFound` (không leak thông tin)
- Map sang `OrderDetailDto`

#### Error cần thêm vào `BookErrors.cs`
```csharp
public static Error InsufficientStock(Guid bookId)
    => Error.Validation("Book.InsufficientStock", $"Book '{bookId}' does not have sufficient stock.");
```

### API Layer — Cần tạo

**`OrdersController`** — `/api/orders`

| Method | Route | Auth | Body/Params | Response |
|--------|-------|------|-------------|----------|
| POST | `/api/orders` | Customer | `CreateOrderCommand` | 201 + OrderId |
| GET | `/api/orders` | Customer | `[FromQuery] GetOrdersQuery` | 200 + `PagedResult<OrderDto>` |
| GET | `/api/orders/{id}` | Customer / Admin | — | 200 + `OrderDetailDto` |
| PATCH | `/api/orders/{id}/confirm` | Admin | — | 200 |
| PATCH | `/api/orders/{id}/ship` | Admin | — | 200 |
| PATCH | `/api/orders/{id}/deliver` | Admin | — | 200 |
| PATCH | `/api/orders/{id}/cancel` | Customer / Admin | — | 200 |

**Lấy userId từ JWT:**
```csharp
private Guid CurrentUserId =>
    Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

private bool IsAdmin =>
    User.IsInRole("Admin");
```

### DI Registration
Thêm vào `Infrastructure/DependencyInjection.cs` (hoặc `ServiceExtensions`):
```csharp
services.AddScoped<IOrderRepository, OrderRepository>();
services.AddScoped<IOrderCommandService, OrderCommandService>();
services.AddScoped<IOrderQueryService, OrderQueryService>();
```

---

## Validation Plan

| Rule | Tầng | Công cụ |
|------|------|---------|
| `ShippingAddress` required, maxLength 500 | API | FluentValidation |
| `Note` optional, maxLength 1000 | API | FluentValidation |
| `Items` không rỗng, `Quantity` ≥ 1 | API | FluentValidation |
| `BookId` phải là Guid hợp lệ | API | FluentValidation |
| Book tồn tại trong DB | Application | Business rule (Service) |
| Book đủ stock | Application | `book.TryReduceStock()` → Failure |
| Items không rỗng (invariant) | Application | `OrderErrors.Empty` |
| State transition hợp lệ | Domain | `Order.Confirm/Ship/Deliver/Cancel()` |
| Ownership (Customer chỉ xem/cancel đơn của mình) | Application | `order.UserId == requesterId` |

**Validator cần tạo:**
- `CreateOrderCommandValidator` → `API/Validators/Orders/`

---

## Testing Strategy

### Unit Test — `OrderCommandServiceTests`
- `CreateAsync_ShouldReturnOrderId_WhenValid`
- `CreateAsync_ShouldFail_WhenBookNotFound`
- `CreateAsync_ShouldFail_WhenInsufficientStock`
- `CreateAsync_ShouldFail_WhenItemsEmpty`
- `ConfirmAsync_ShouldSucceed_WhenPending`
- `ConfirmAsync_ShouldFail_WhenAlreadyConfirmed` (domain guards)
- `CancelAsync_ShouldRestoreStock_WhenPendingOrConfirmed`
- `CancelAsync_ShouldFail_WhenShipped` (domain guards)
- `CancelAsync_ShouldFail_WhenCustomerCancelsOthersOrder`

**Mock cần:** `IOrderRepository`, `IBookRepository`, `IUnitOfWork`

### Unit Test — `OrderTests` (Domain state machine)
- `Confirm_ShouldSucceed_WhenPending`
- `Confirm_ShouldFail_WhenNotPending` → `[Theory]` InlineData(Confirmed, Shipped, Delivered, Cancelled)
- `Ship_ShouldSucceed_WhenConfirmed`
- `Ship_ShouldFail_WhenNotConfirmed`
- `Deliver_ShouldSucceed_WhenShipped`
- `Deliver_ShouldFail_WhenNotShipped`
- `Cancel_ShouldSucceed_WhenPending`
- `Cancel_ShouldSucceed_WhenConfirmed`
- `Cancel_ShouldFail_WhenShipped` → `[Theory]` InlineData(Shipped, Delivered, Cancelled)
- `AddItem_ShouldFail_WhenNotPending`

---

## Boundaries

### Always Do
- Result Pattern — không throw exception cho lỗi nghiệp vụ
- Service gọi domain method → propagate `Result` — không tự kiểm tra trạng thái
- `order.Cancel()` + `book.RestoreStock()` trong cùng 1 `SaveChangesAsync`
- Customer chỉ thao tác đơn của chính mình (ownership check ở Service, không ở Controller)
- Controller lấy `userId` từ `ClaimTypes.NameIdentifier`, không nhận từ request body

### Ask First
- Thêm `OrderNumber` (human-readable) — nếu cần cho UX
- Thêm `UpdatedAt` tracking per-item
- Payment webhook / partial refund khi cancel

### Never Do
- `if (order.Status == OrderStatus.Pending)` trong Service → push xuống Domain
- Gọi `order.Status = ...` trực tiếp từ Service (bypass state machine)
- Customer truyền `userId` trong body để tạo order — luôn lấy từ JWT

---

## File Checklist (tất cả cần tạo mới)

```
Domain/IRepository/
  └── IOrderRepository.cs

Infrastructure/Repository/
  └── OrderRepository.cs

Application/Orders/
  ├── Commands/
  │   ├── CreateOrderCommand.cs
  │   └── CreateOrderItemCommand.cs
  ├── Queries/
  │   └── GetOrdersQuery.cs
  ├── DTOs/
  │   ├── OrderDto.cs
  │   ├── OrderDetailDto.cs
  │   └── OrderItemDto.cs
  ├── IService/
  │   ├── IOrderCommandService.cs
  │   └── IOrderQueryService.cs
  └── Services/
      ├── OrderCommandService.cs
      └── OrderQueryService.cs

API/
  ├── Controllers/
  │   └── OrdersController.cs
  └── Validators/Orders/
      └── CreateOrderCommandValidator.cs

BookStore.Domain/Errors/
  └── BookErrors.cs (thêm InsufficientStock)

tests/BookStore.UnitTests/
  ├── Application/Orders/
  │   └── OrderCommandServiceTests.cs
  └── Domain/Orders/
      └── OrderTests.cs
```
