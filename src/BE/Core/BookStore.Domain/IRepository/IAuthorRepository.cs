using BookStore.Domain.Entities;

namespace BookStore.Domain.IRepository;

public interface IAuthorRepository
{
    Task<Author?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Author?> GetByIdWithBooksAsync(Guid id, CancellationToken ct = default);
    Task<bool> ExistsByFullNameAsync(string fullName, CancellationToken ct = default);
    Task<bool> HasBooksAsync(Guid id, CancellationToken ct = default);
    IQueryable<Author> GetQueryable();
    void Add(Author author);
    void Remove(Author author);
}
