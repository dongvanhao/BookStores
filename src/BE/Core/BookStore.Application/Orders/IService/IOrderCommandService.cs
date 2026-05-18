using BookStore.Application.Orders.Commands;
using BookStore.Shared.Results;

namespace BookStore.Application.Orders.IService;

public interface IOrderCommandService
{
    Task<Result<Guid>> CreateAsync(CreateOrderCommand cmd, Guid userId, CancellationToken ct = default);
    Task<Result>       ConfirmAsync(Guid orderId, CancellationToken ct = default);
    Task<Result>       ShipAsync(Guid orderId, CancellationToken ct = default);
    Task<Result>       DeliverAsync(Guid orderId, CancellationToken ct = default);
    Task<Result>       CancelAsync(Guid orderId, Guid requesterId, bool isAdmin, CancellationToken ct = default);
}
