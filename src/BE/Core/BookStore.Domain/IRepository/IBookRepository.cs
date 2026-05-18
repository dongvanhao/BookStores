using BookStore.Domain.Entities;

namespace BookStore.Domain.IRepository;

public interface IBookRepository
{
    Task<Book?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Book?> GetWithDetailsAsync(Guid id, CancellationToken ct = default);
    Task<bool> ExistsByTitleAsync(string title, CancellationToken ct = default);
    Task<bool> ExistsByISBNAsync(string isbn, CancellationToken ct = default);
    IQueryable<Book> GetQueryable();
    IQueryable<Book> GetQueryableWithDetails(); // includes Category, BookAuthors.Author
    void Add(Book book);
    void Remove(Book book);

    Task<List<Guid>> GetAuthorIdsAsync(Guid bookId, CancellationToken ct = default);
    void AddBookAuthor(BookAuthor bookAuthor);
    void RemoveBookAuthor(BookAuthor bookAuthor);
}
