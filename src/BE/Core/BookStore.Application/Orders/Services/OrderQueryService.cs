using BookStore.Application.Orders.DTOs;
using BookStore.Application.Orders.IService;
using BookStore.Application.Orders.Queries;
using BookStore.Domain.Errors;
using BookStore.Domain.IRepository;
using BookStore.Shared.Common;
using BookStore.Shared.Extensions;
using BookStore.Shared.Results;

namespace BookStore.Application.Orders.Services;

public class OrderQueryService(IOrderRepository orderRepo) : IOrderQueryService
{
    public async Task<Result<PagedResult<OrderDto>>> GetOrderHistoryAsync(
        GetOrdersQuery request, Guid userId, CancellationToken ct = default)
    {
        var query = orderRepo.GetQueryable()
            .Where(o => o.UserId == userId);

        if (request.Status.HasValue)
            query = query.Where(o => o.Status == request.Status.Value);

        var projected = query.Select(o => new OrderDto(
            o.Id,
            o.Status.ToString(),
            o.TotalAmount,
            o.Items.Count,
            o.ShippingAddress,
            o.CreatedAt));

        var paged = await projected
            .ApplySort(request.SortBy, request.IsAscending)
            .ToPagedResultAsync(request, ct);

        return paged;
    }

    public async Task<Result<OrderDetailDto>> GetByIdAsync(
        Guid orderId, Guid requesterId, bool isAdmin, CancellationToken ct = default)
    {
        var order = await orderRepo.GetByIdWithItemsAsync(orderId, ct);
        if (order is null) return OrderErrors.NotFound(orderId);

        // Ownership check — return same 404 to avoid leaking existence
        if (!isAdmin && order.UserId != requesterId)
            return OrderErrors.NotFound(orderId);

        var items = order.Items
            .Select(i => new OrderItemDto(i.BookId, i.BookTitle, i.Quantity, i.UnitPrice, i.SubTotal))
            .ToList();

        return new OrderDetailDto(
            order.Id,
            order.Status.ToString(),
            order.TotalAmount,
            order.ShippingAddress,
            order.Note,
            order.CreatedAt,
            items);
    }
}
