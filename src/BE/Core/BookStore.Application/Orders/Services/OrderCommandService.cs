using BookStore.Application.Orders.Commands;
using BookStore.Application.Orders.IService;
using BookStore.Domain.Entities;
using BookStore.Domain.Errors;
using BookStore.Domain.IRepository;
using BookStore.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Application.Orders.Services;

public class OrderCommandService(
    IOrderRepository orderRepo,
    IBookRepository  bookRepo,
    IUnitOfWork      unitOfWork) : IOrderCommandService
{
    public async Task<Result<Guid>> CreateAsync(CreateOrderCommand cmd, Guid userId, CancellationToken ct = default)
    {
        if (cmd.Items.Count == 0)
            return OrderErrors.Empty;

        var bookIds = cmd.Items.Select(i => i.BookId).ToList();
        var books   = await bookRepo.GetQueryable()
            .Where(b => bookIds.Contains(b.Id))
            .ToListAsync(ct);

        var order = Order.Create(userId, cmd.ShippingAddress, cmd.Note);

        foreach (var item in cmd.Items)
        {
            var book = books.FirstOrDefault(b => b.Id == item.BookId);
            if (book is null)
                return BookErrors.NotFound(item.BookId);

            if (!book.TryReduceStock(item.Quantity))
                return BookErrors.InsufficientStock(item.BookId);

            var addResult = order.AddItem(book.Id, book.Title, item.Quantity, book.Price);
            if (!addResult.IsSuccess)
                return addResult.Error;
        }

        orderRepo.Add(order);
        await unitOfWork.SaveChangesAsync(ct);
        return order.Id;
    }

    public async Task<Result> ConfirmAsync(Guid orderId, CancellationToken ct = default)
    {
        var order = await orderRepo.GetByIdAsync(orderId, ct);
        if (order is null) return Result.Failure(OrderErrors.NotFound(orderId));
        var result = order.Confirm();
        if (!result.IsSuccess) return result;
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> ShipAsync(Guid orderId, CancellationToken ct = default)
    {
        var order = await orderRepo.GetByIdAsync(orderId, ct);
        if (order is null) return Result.Failure(OrderErrors.NotFound(orderId));
        var result = order.Ship();
        if (!result.IsSuccess) return result;
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> DeliverAsync(Guid orderId, CancellationToken ct = default)
    {
        var order = await orderRepo.GetByIdAsync(orderId, ct);
        if (order is null) return Result.Failure(OrderErrors.NotFound(orderId));
        var result = order.Deliver();
        if (!result.IsSuccess) return result;
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> CancelAsync(Guid orderId, Guid requesterId, bool isAdmin, CancellationToken ct = default)
    {
        // Need WithItems to restore stock
        var order = await orderRepo.GetByIdWithItemsAsync(orderId, ct);
        if (order is null) return Result.Failure(OrderErrors.NotFound(orderId));

        // Ownership check: customer can only cancel their own orders
        if (!isAdmin && order.UserId != requesterId)
            return Result.Failure(OrderErrors.NotFound(orderId));

        // Domain guards the transition (Pending/Confirmed → OK, Shipped/Delivered/Cancelled → Failure)
        var result = order.Cancel();
        if (!result.IsSuccess) return result;

        // Restore stock atomically with the cancel
        var bookIds = order.Items.Select(i => i.BookId).ToList();
        var books = await bookRepo.GetQueryable()
            .Where(b => bookIds.Contains(b.Id))
            .ToListAsync(ct);

        foreach (var item in order.Items)
        {
            var book = books.FirstOrDefault(b => b.Id == item.BookId);
            book?.RestoreStock(item.Quantity);
        }

        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}
