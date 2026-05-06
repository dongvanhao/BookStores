using BookStore.Domain.Entities;

namespace BookStore.Domain.IRepository;

public interface ICategoryRepository
{
    Task<Category?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Category?> GetByIdWithChildrenAsync(Guid id, CancellationToken ct = default);
    Task<List<Category>> GetAllWithChildrenAsync(CancellationToken ct = default);
    IQueryable<Category> GetQueryable();
    Task<bool> HasChildrenAsync(Guid id, CancellationToken ct = default);
    Task<bool> HasBooksAsync(Guid id, CancellationToken ct = default);
    Task<List<Guid>> GetDescendantIdsAsync(Guid id, CancellationToken ct = default);
    void Add(Category category);
    void Remove(Category category);
}
