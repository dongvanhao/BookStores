using BookStore.Domain.IRepository;
using BookStore.Infrastructure.Data;

namespace BookStore.Infrastructure.Repository;

public class UnitOfWork(AppDbContext context) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => context.SaveChangesAsync(ct);
}
