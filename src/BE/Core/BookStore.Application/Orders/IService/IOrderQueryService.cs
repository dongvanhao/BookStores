using BookStore.Application.Orders.DTOs;
using BookStore.Application.Orders.Queries;
using BookStore.Shared.Common;
using BookStore.Shared.Results;

namespace BookStore.Application.Orders.IService;

public interface IOrderQueryService
{
    Task<Result<PagedResult<OrderDto>>> GetOrderHistoryAsync(
        GetOrdersQuery query, Guid userId, CancellationToken ct = default);

    Task<Result<OrderDetailDto>> GetByIdAsync(
        Guid orderId, Guid requesterId, bool isAdmin, CancellationToken ct = default);
}
