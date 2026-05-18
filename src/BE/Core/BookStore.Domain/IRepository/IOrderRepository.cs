using BookStore.Domain.Entities;

namespace BookStore.Domain.IRepository;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Order?> GetByIdWithItemsAsync(Guid id, CancellationToken ct = default);
    IQueryable<Order> GetQueryable();
    void Add(Order order);
}
