# TODO: Orders Module

> Spec: `docs/specs/orders-module.md`  
> Branch: `feature/add-user-module`  
> Nguyên tắc cốt lõi: Service KHÔNG kiểm tra trạng thái — push xuống Domain

---

## Slice 0 — Domain Tests (Safety Net)

> **Mục tiêu:** Verify state machine đã có sẵn trước khi build bên trên.  
> Domain đã done → tests phải GREEN ngay từ đầu. Nếu fail → fix Domain trước.

- [ ] **Task 0.1** — `OrderTests.cs` — State machine

  **Files:**
  - `tests/BookStore.UnitTests/Domain/Orders/OrderTests.cs` *(tạo mới)*

  **Test cases:**
  ```
  // Confirm
  Confirm_ShouldSucceed_WhenPending
  Confirm_ShouldFail_WhenNotPending [Theory: Confirmed, Shipped, Delivered, Cancelled]

  // Ship
  Ship_ShouldSucceed_WhenConfirmed
  Ship_ShouldFail_WhenNotConfirmed [Theory: Pending, Shipped, Delivered, Cancelled]

  // Deliver
  Deliver_ShouldSucceed_WhenShipped
  Deliver_ShouldFail_WhenNotShipped [Theory: Pending, Confirmed, Delivered, Cancelled]

  // Cancel
  Cancel_ShouldSucceed_WhenPending
  Cancel_ShouldSucceed_WhenConfirmed
  Cancel_ShouldFail_WhenShipped [Theory: Shipped, Delivered, Cancelled]

  // AddItem
  AddItem_ShouldFail_WhenNotPending
  AddItem_ShouldRecalculateTotal_WhenAdded
  ```

  **Acceptance:**
  - [ ] `dotnet test --filter "FullyQualifiedName~OrderTests"` — tất cả GREEN
  - [ ] Không có logic nào trong test — chỉ gọi domain method, assert `Result`

---

## Checkpoint 0 — Domain verified

```
[ ] dotnet test Domain/Orders → tất cả GREEN
[ ] Không sửa Domain entity — chỉ viết test
```

---

## Slice 1 — Create Order

> **Mục tiêu:** Customer `POST /api/orders` → giảm stock → tạo Order ở Pending.  
> Đây là slice phức tạp nhất vì cần coordinate Orders + Books + stock atomically.

- [ ] **Task 1.1** — `IOrderRepository` + `BookErrors.InsufficientStock`

  **Files:**
  - `BookStore.Domain/IRepository/IOrderRepository.cs` *(tạo mới)*
  - `BookStore.Domain/Errors/BookErrors.cs` *(thêm 1 error)*

  **`IOrderRepository` contract:**
  ```csharp
  Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default);
  Task<Order?> GetByIdWithItemsAsync(Guid id, CancellationToken ct = default);
  IQueryable<Order> GetQueryable();
  void Add(Order order);
  ```

  **`BookErrors` — thêm:**
  ```csharp
  public static Error InsufficientStock(Guid bookId)
      => Error.Validation("Book.InsufficientStock",
          $"Book '{bookId}' does not have sufficient stock.");
  ```

  **Acceptance:**
  - [ ] Interface chỉ nằm trong Domain layer — không import Infrastructure
  - [ ] `dotnet build` sạch

- [ ] **Task 1.2** — `OrderRepository` (Infrastructure)

  **Files:**
  - `BookStore.Infrastructure/Repository/OrderRepository.cs` *(tạo mới)*

  **Implementation notes:**
  ```csharp
  // GetByIdWithItemsAsync — eager load items + book (cần cho detail + cancel)
  .Include(o => o.Items)
      .ThenInclude(i => i.Book)
  .FirstOrDefaultAsync(o => o.Id == id, ct)

  // GetQueryable — không Include (dùng cho paged query + projection)
  return _context.Orders.AsQueryable();
  ```

  **Acceptance:**
  - [ ] `dotnet build` sạch

- [ ] **Task 1.3** — Commands + Validator

  **Files:**
  - `Application/Orders/Commands/CreateOrderCommand.cs` *(tạo mới)*
  - `Application/Orders/Commands/CreateOrderItemCommand.cs` *(tạo mới)*
  - `BookStore.API/Validators/Orders/CreateOrderCommandValidator.cs` *(tạo mới)*

  **Structures:**
  ```csharp
  public record CreateOrderCommand(
      string ShippingAddress,
      string? Note,
      IReadOnlyList<CreateOrderItemCommand> Items
  );

  public record CreateOrderItemCommand(Guid BookId, int Quantity);
  ```

  **Validator rules:**
  ```
  ShippingAddress: NotEmpty, MaxLength(500)
  Note: optional, MaxLength(1000)
  Items: NotEmpty (ít nhất 1 item)
  Items[].BookId: NotEmpty
  Items[].Quantity: GreaterThan(0)
  ```

  **Acceptance:**
  - [ ] Mọi rule có `.WithMessage("...")` kết thúc bằng dấu chấm
  - [ ] Không dùng `.Must()` cho rule đã có sẵn

- [ ] **Task 1.4** — `IOrderCommandService` + `OrderCommandService.CreateAsync`

  **Files:**
  - `Application/Orders/IService/IOrderCommandService.cs` *(tạo mới)*
  - `Application/Orders/Services/OrderCommandService.cs` *(tạo mới — chỉ implement CreateAsync)*

  **Interface (toàn bộ, dù chưa implement hết):**
  ```csharp
  Task<Result<Guid>> CreateAsync(CreateOrderCommand cmd, Guid userId, CancellationToken ct = default);
  Task<Result>       ConfirmAsync(Guid orderId, CancellationToken ct = default);
  Task<Result>       ShipAsync(Guid orderId, CancellationToken ct = default);
  Task<Result>       DeliverAsync(Guid orderId, CancellationToken ct = default);
  Task<Result>       CancelAsync(Guid orderId, Guid requesterId, bool isAdmin, CancellationToken ct = default);
  ```

  **`CreateAsync` flow — theo thứ tự:**
  1. Guard: `cmd.Items` rỗng → `OrderErrors.Empty`
  2. Batch-fetch books: `_bookRepo.GetQueryable().Where(b => bookIds.Contains(b.Id)).ToListAsync()`
  3. Validate từng item: book không tồn tại → `BookErrors.NotFound(bookId)`
  4. `book.TryReduceStock(qty)` — false → `BookErrors.InsufficientStock(bookId)`
  5. `Order.Create(userId, shippingAddress, note)`
  6. Foreach item: `order.AddItem(book.Id, book.Title, qty, book.Price)` → propagate nếu fail
  7. `_orderRepo.Add(order)`
  8. `await _unitOfWork.SaveChangesAsync(ct)`
  9. `return order.Id`

  **SOLID checklist:**
  - [ ] SRP: Service chỉ orchestrate — không có log/email trực tiếp
  - [ ] DIP: inject `IOrderRepository`, `IBookRepository`, `IUnitOfWork` qua constructor
  - [ ] Không `new` dependency

- [ ] **Task 1.5** — `POST /api/orders` endpoint + DI

  **Files:**
  - `BookStore.API/Controllers/OrdersController.cs` *(tạo mới)*
  - `BookStore.API/Extensions/ServiceExtensions.cs` *(thêm DI)*

  **Controller skeleton:**
  ```csharp
  [Route("api/orders")]
  [ApiController]
  [Authorize]
  public class OrdersController(
      IOrderCommandService commandService,
      IOrderQueryService queryService) : BaseController
  {
      private Guid CurrentUserId =>
          Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

      private bool IsAdmin => User.IsInRole("Admin");

      [HttpPost]
      public async Task<IActionResult> Create(CreateOrderCommand cmd, CancellationToken ct)
          => HandleCreated(await commandService.CreateAsync(cmd, CurrentUserId, ct), nameof(GetById));
  }
  ```

  **DI — thêm vào `AddApplicationServices`:**
  ```csharp
  services.AddScoped<IOrderRepository, OrderRepository>();
  services.AddScoped<IOrderCommandService, OrderCommandService>();
  ```
  *(IOrderQueryService thêm ở Slice 2)*

  **Acceptance:**
  - [ ] `[Authorize]` ở class level — tất cả endpoints yêu cầu login
  - [ ] `userId` lấy từ JWT — không nhận từ request body
  - [ ] Swagger XML doc có trên endpoint

- [ ] **Task 1.6** — Unit tests `CreateAsync`

  **Files:**
  - `tests/BookStore.UnitTests/Application/Orders/OrderCommandServiceTests.cs` *(tạo mới)*

  **Test cases:**
  ```
  CreateAsync_ShouldReturnOrderId_WhenAllBooksExistAndHaveStock
  CreateAsync_ShouldFail_WhenItemsListIsEmpty
  CreateAsync_ShouldFail_WhenBookNotFound
  CreateAsync_ShouldFail_WhenInsufficientStock
  CreateAsync_ShouldReduceStockForAllItems_WhenSuccessful
  CreateAsync_ShouldCallSaveChanges_WhenSuccessful
  ```

  **Mock:** `IOrderRepository`, `IBookRepository`, `IUnitOfWork`

---

## Checkpoint 1 — Create Order working

```
[ ] POST /api/orders → 201 Created với OrderId
[ ] POST /api/orders với items rỗng → 400 Validation
[ ] POST /api/orders với book không tồn tại → 404
[ ] POST /api/orders với hết stock → 400 Book.InsufficientStock
[ ] dotnet build sạch
[ ] dotnet test --filter "CreateAsync" → GREEN
```

---

## Slice 2 — View Orders (History + Detail)

> **Mục tiêu:** Customer xem lịch sử và chi tiết đơn hàng của mình.

- [ ] **Task 2.1** — DTOs + Query

  **Files:**
  - `Application/Orders/DTOs/OrderDto.cs` *(tạo mới)*
  - `Application/Orders/DTOs/OrderDetailDto.cs` *(tạo mới)*
  - `Application/Orders/DTOs/OrderItemDto.cs` *(tạo mới)*
  - `Application/Orders/Queries/GetOrdersQuery.cs` *(tạo mới)*

  **Structures:**
  ```csharp
  public record OrderDto(
      Guid Id,
      string Status,
      decimal TotalAmount,
      int ItemCount,
      string ShippingAddress,
      DateTime CreatedAt
  );

  public record OrderDetailDto(
      Guid Id,
      string Status,
      decimal TotalAmount,
      string ShippingAddress,
      string? Note,
      DateTime CreatedAt,
      IReadOnlyList<OrderItemDto> Items
  );

  public record OrderItemDto(
      Guid BookId,
      string BookTitle,
      int Quantity,
      decimal UnitPrice,
      decimal SubTotal
  );

  public sealed class GetOrdersQuery : QueryParams
  {
      public OrderStatus? Status { get; set; }
  }
  ```

- [ ] **Task 2.2** — `IOrderQueryService` + `OrderQueryService`

  **Files:**
  - `Application/Orders/IService/IOrderQueryService.cs` *(tạo mới)*
  - `Application/Orders/Services/OrderQueryService.cs` *(tạo mới)*

  **Interface:**
  ```csharp
  Task<Result<PagedResult<OrderDto>>> GetOrderHistoryAsync(
      GetOrdersQuery query, Guid userId, CancellationToken ct = default);

  Task<Result<OrderDetailDto>> GetByIdAsync(
      Guid orderId, Guid requesterId, bool isAdmin, CancellationToken ct = default);
  ```

  **`GetOrderHistoryAsync` flow:**
  ```csharp
  _orderRepo.GetQueryable()
      .Where(o => o.UserId == userId)
      // filter status nếu có
      .Select(o => new OrderDto(
          o.Id, o.Status.ToString(), o.TotalAmount,
          o.Items.Count, o.ShippingAddress, o.CreatedAt))
      .ApplySort(query.SortBy, query.IsAscending)
      .ToPagedResultAsync(query, ct)
  ```

  **`GetByIdAsync` flow:**
  ```csharp
  var order = await _orderRepo.GetByIdWithItemsAsync(orderId, ct);
  if (order is null) return OrderErrors.NotFound(orderId);
  if (!isAdmin && order.UserId != requesterId)
      return OrderErrors.NotFound(orderId);  // không leak — cùng 404
  return MapToDetailDto(order);
  ```

- [ ] **Task 2.3** — `GET /api/orders` + `GET /api/orders/{id}`

  **Files:**
  - `BookStore.API/Controllers/OrdersController.cs` *(thêm actions)*
  - `BookStore.API/Extensions/ServiceExtensions.cs` *(thêm IOrderQueryService)*

  **Actions:**
  ```csharp
  [HttpGet]
  public async Task<IActionResult> GetHistory(
      [FromQuery] GetOrdersQuery query, CancellationToken ct)
      => HandlePagedResult(
          await queryService.GetOrderHistoryAsync(query, CurrentUserId, ct));

  [HttpGet("{id:guid}")]
  public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
      => HandleResult(
          await queryService.GetByIdAsync(id, CurrentUserId, IsAdmin, ct));
  ```

  **Acceptance:**
  - [ ] Customer xem đơn người khác → 404 (không phải 403)
  - [ ] Pagination đúng format `PagedResult<OrderDto>`

- [ ] **Task 2.4** — Unit tests Query

  **Files:**
  - `tests/BookStore.UnitTests/Application/Orders/OrderQueryServiceTests.cs` *(tạo mới)*

  **Test cases:**
  ```
  GetOrderHistoryAsync_ShouldReturnOnlyCurrentUserOrders
  GetOrderHistoryAsync_ShouldFilterByStatus_WhenProvided
  GetByIdAsync_ShouldReturnDetail_WhenOwner
  GetByIdAsync_ShouldReturnDetail_WhenAdmin
  GetByIdAsync_ShouldReturnNotFound_WhenCustomerAccessesOthersOrder
  GetByIdAsync_ShouldReturnNotFound_WhenOrderDoesNotExist
  ```

---

## Checkpoint 2 — Read Orders working

```
[ ] GET /api/orders → 200 PagedResult (chỉ thấy đơn của mình)
[ ] GET /api/orders?status=Pending → filter đúng
[ ] GET /api/orders/{id} (own) → 200 với Items
[ ] GET /api/orders/{id} (others, Customer) → 404
[ ] GET /api/orders/{id} (Admin) → 200 dù order của ai
[ ] dotnet test --filter "OrderQueryService" → GREEN
```

---

## Slice 3 — Admin Pipeline (Confirm / Ship / Deliver)

> **Mục tiêu:** Admin đẩy đơn hàng qua các bước sau Pending.  
> Service chỉ có 3 dòng/method — domain tự guard.

- [ ] **Task 3.1** — `ConfirmAsync` + `ShipAsync` + `DeliverAsync`

  **Files:**
  - `Application/Orders/Services/OrderCommandService.cs` *(thêm 3 methods)*

  **Pattern cho cả 3 (ví dụ Confirm):**
  ```csharp
  public async Task<Result> ConfirmAsync(Guid orderId, CancellationToken ct)
  {
      var order = await _orderRepo.GetByIdAsync(orderId, ct);
      if (order is null) return OrderErrors.NotFound(orderId);
      var result = order.Confirm();            // domain guard
      if (result.IsFailure) return result;
      await _unitOfWork.SaveChangesAsync(ct);
      return Result.Success();
  }
  ```

  **Acceptance:**
  - [ ] Không có `if (order.Status == ...)` trong Service
  - [ ] `ShipAsync` và `DeliverAsync` cùng pattern

- [ ] **Task 3.2** — `PATCH /api/orders/{id}/confirm|ship|deliver`

  **Files:**
  - `BookStore.API/Controllers/OrdersController.cs` *(thêm 3 actions)*

  **Actions:**
  ```csharp
  [HttpPatch("{id:guid}/confirm")]
  [Authorize(Roles = "Admin")]
  public async Task<IActionResult> Confirm(Guid id, CancellationToken ct)
      => HandleResult(await commandService.ConfirmAsync(id, ct));

  [HttpPatch("{id:guid}/ship")]
  [Authorize(Roles = "Admin")]
  public async Task<IActionResult> Ship(Guid id, CancellationToken ct)
      => HandleResult(await commandService.ShipAsync(id, ct));

  [HttpPatch("{id:guid}/deliver")]
  [Authorize(Roles = "Admin")]
  public async Task<IActionResult> Deliver(Guid id, CancellationToken ct)
      => HandleResult(await commandService.DeliverAsync(id, ct));
  ```

- [ ] **Task 3.3** — Unit tests transitions + Domain tests

  **Files:**
  - `tests/BookStore.UnitTests/Application/Orders/OrderCommandServiceTests.cs` *(thêm)*
  - `tests/BookStore.UnitTests/Domain/Orders/OrderTests.cs` *(đã có từ Slice 0)*

  **Service test cases:**
  ```
  ConfirmAsync_ShouldSucceed_WhenOrderIsPending
  ConfirmAsync_ShouldFail_WhenOrderNotFound
  ConfirmAsync_ShouldFail_WhenOrderNotPending  (domain trả Failure)
  ShipAsync_ShouldSucceed_WhenOrderIsConfirmed
  ShipAsync_ShouldFail_WhenOrderNotConfirmed
  DeliverAsync_ShouldSucceed_WhenOrderIsShipped
  DeliverAsync_ShouldFail_WhenOrderNotShipped
  ```

---

## Checkpoint 3 — Admin Pipeline working

```
[ ] PATCH /api/orders/{id}/confirm (Admin, Pending) → 200
[ ] PATCH /api/orders/{id}/confirm (Admin, Confirmed) → 400 Order.InvalidTransition
[ ] PATCH /api/orders/{id}/ship (Admin, Confirmed) → 200
[ ] PATCH /api/orders/{id}/deliver (Admin, Shipped) → 200
[ ] PATCH /api/orders/{id}/confirm (Customer) → 403
[ ] dotnet test --filter "ConfirmAsync|ShipAsync|DeliverAsync" → GREEN
```

---

## Slice 4 — Cancel Order (với Stock Restore)

> **Mục tiêu:** Admin hoặc Customer hủy đơn. Stock được hoàn trả nguyên tử.  
> Đây là slice phức tạp nhất vì cần items + book entities cùng lúc.

- [ ] **Task 4.1** — `CancelAsync` với stock restore

  **Files:**
  - `Application/Orders/Services/OrderCommandService.cs` *(thêm CancelAsync)*

  **`CancelAsync` flow:**
  ```csharp
  public async Task<Result> CancelAsync(
      Guid orderId, Guid requesterId, bool isAdmin, CancellationToken ct)
  {
      // Cần WithItems để restore stock
      var order = await _orderRepo.GetByIdWithItemsAsync(orderId, ct);
      if (order is null) return OrderErrors.NotFound(orderId);

      // Ownership check — Customer chỉ cancel đơn của mình
      if (!isAdmin && order.UserId != requesterId)
          return OrderErrors.NotFound(orderId);

      // Domain guard — không cần if/else ở đây
      var result = order.Cancel();
      if (result.IsFailure) return result;

      // Restore stock cho tất cả items (chỉ khi Cancel thành công)
      var bookIds = order.Items.Select(i => i.BookId).ToList();
      var books = await _bookRepo.GetQueryable()
          .Where(b => bookIds.Contains(b.Id))
          .ToListAsync(ct);

      foreach (var item in order.Items)
      {
          var book = books.FirstOrDefault(b => b.Id == item.BookId);
          book?.RestoreStock(item.Quantity);
      }

      await _unitOfWork.SaveChangesAsync(ct);
      return Result.Success();
  }
  ```

- [ ] **Task 4.2** — `PATCH /api/orders/{id}/cancel`

  **Files:**
  - `BookStore.API/Controllers/OrdersController.cs` *(thêm action)*

  **Action:**
  ```csharp
  [HttpPatch("{id:guid}/cancel")]
  public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
      => HandleResult(
          await commandService.CancelAsync(id, CurrentUserId, IsAdmin, ct));
  ```
  *(Không cần `[Authorize(Roles)]` riêng — class đã có `[Authorize]`, cả Customer lẫn Admin vào được)*

- [ ] **Task 4.3** — Unit tests Cancel + stock restore

  **Files:**
  - `tests/BookStore.UnitTests/Application/Orders/OrderCommandServiceTests.cs` *(thêm)*

  **Test cases:**
  ```
  CancelAsync_ShouldSucceed_AndRestoreStock_WhenPending
  CancelAsync_ShouldSucceed_AndRestoreStock_WhenConfirmed
  CancelAsync_ShouldFail_WhenOrderNotFound
  CancelAsync_ShouldFail_WhenOrderIsShipped  (domain trả Failure)
  CancelAsync_ShouldFail_WhenCustomerCancelsOthersOrder
  CancelAsync_ShouldRestoreStockForAllItems  (verify book.RestoreStock được gọi)
  CancelAsync_ShouldNotRestoreStock_WhenCancelFails
  ```

---

## Checkpoint 4 — Cancel working

```
[ ] PATCH /api/orders/{id}/cancel (Customer, own Pending) → 200
[ ] PATCH /api/orders/{id}/cancel (Customer, own Confirmed) → 200
[ ] PATCH /api/orders/{id}/cancel (Customer, others order) → 404
[ ] PATCH /api/orders/{id}/cancel (Admin, any Pending/Confirmed) → 200
[ ] PATCH /api/orders/{id}/cancel (Shipped order) → 400 Order.CannotCancel
[ ] Stock của Books được restore sau khi cancel
[ ] dotnet test --filter "CancelAsync" → GREEN
```

---

## Final Checkpoint — Module Complete

```
[ ] dotnet build — 0 errors, 0 warnings
[ ] dotnet test tests/BookStore.UnitTests — tất cả GREEN
[ ] Swagger hiển thị đủ 7 endpoints /api/orders
[ ] Dependency rule: Domain không import Application/Infrastructure
[ ] Không có throw exception cho lỗi nghiệp vụ (grep "throw new" trong Orders)
[ ] Không có if/else kiểm tra OrderStatus trong Service layer
[ ] Mọi Controller action kết thúc bằng HandleResult / HandleCreated / HandlePagedResult
```

---

## Execution Order Summary

```
Slice 0  →  Domain Tests (verify existing)            [~30 min]
Slice 1  →  Create Order (complex, phụ thuộc nhiều)  [~60 min]
Slice 2  →  View Orders (đọc, đơn giản hơn)          [~40 min]
Slice 3  →  Admin Pipeline (confirm/ship/deliver)     [~30 min]
Slice 4  →  Cancel + Stock Restore (atomic)           [~30 min]
```

> Mỗi Slice buildable và testable độc lập.  
> Checkpoint pass → mới chuyển Slice tiếp theo.  
> Dùng `/build <task>` để implement từng task.
