namespace BookStore.Domain.IRepository.Common
{
    public interface IDbSession
    {
        Task<int> SaveChangesAsync(CancellationToken ct = default);
        Task ExecuteTransactionAsync(Func<Task> action);
    }
}
