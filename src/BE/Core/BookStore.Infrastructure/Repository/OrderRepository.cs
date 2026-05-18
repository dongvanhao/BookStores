using BookStore.Domain.Entities;
using BookStore.Domain.IRepository;
using BookStore.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Infrastructure.Repository;

public class OrderRepository(AppDbContext context) : IOrderRepository
{
    public Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => context.Orders.FirstOrDefaultAsync(o => o.Id == id, ct);

    public Task<Order?> GetByIdWithItemsAsync(Guid id, CancellationToken ct = default)
        => context.Orders
            .Include(o => o.Items)
                .ThenInclude(i => i.Book)
            .FirstOrDefaultAsync(o => o.Id == id, ct);

    public IQueryable<Order> GetQueryable()
        => context.Orders.AsQueryable();

    public void Add(Order order)
        => context.Orders.Add(order);
}
