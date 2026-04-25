using BookStore.Domain.IRepository.Common;
using BookStore.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Infrastructure.Repository.Common
{
    public class DbSession : IDbSession
    {
        private readonly AppDbContext _ctx;

        public DbSession(AppDbContext ctx) => _ctx = ctx;

        public async Task<int> SaveChangesAsync(CancellationToken ct = default)
            => await _ctx.SaveChangesAsync(ct);

        public async Task ExecuteTransactionAsync(Func<Task> action)
        {
            var strategy = _ctx.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _ctx.Database.BeginTransactionAsync();
                try
                {
                    await action();
                    await transaction.CommitAsync();
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }
    }
}
