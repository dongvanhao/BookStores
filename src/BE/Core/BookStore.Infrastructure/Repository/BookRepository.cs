using BookStore.Domain.Entities;
using BookStore.Domain.IRepository;
using BookStore.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Infrastructure.Repository;

public class BookRepository(AppDbContext context) : IBookRepository
{
    public Task<Book?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => context.Books.FirstOrDefaultAsync(b => b.Id == id, ct);

    public Task<Book?> GetWithDetailsAsync(Guid id, CancellationToken ct = default)
        => context.Books
            .Include(b => b.Category)
            .Include(b => b.BookAuthors).ThenInclude(ba => ba.Author)
            .Include(b => b.Reviews)
            .FirstOrDefaultAsync(b => b.Id == id, ct);

    public Task<bool> ExistsByTitleAsync(string title, CancellationToken ct = default)
        => context.Books.AnyAsync(b => b.Title == title, ct);

    public Task<bool> ExistsByISBNAsync(string isbn, CancellationToken ct = default)
        => context.Books.AnyAsync(b => b.ISBN == isbn, ct);

    public IQueryable<Book> GetQueryable()
        => context.Books.AsQueryable();

    public IQueryable<Book> GetQueryableWithDetails()
        => context.Books
            .Include(b => b.Category)
            .Include(b => b.BookAuthors).ThenInclude(ba => ba.Author)
            .AsQueryable();

    public void Add(Book book)
        => context.Books.Add(book);

    public void Remove(Book book)
        => context.Books.Remove(book);

    public Task<List<Guid>> GetAuthorIdsAsync(Guid bookId, CancellationToken ct = default)
        => context.BookAuthors
            .Where(ba => ba.BookId == bookId)
            .Select(ba => ba.AuthorId)
            .ToListAsync(ct);

    public void AddBookAuthor(BookAuthor bookAuthor)
        => context.BookAuthors.Add(bookAuthor);

    public void RemoveBookAuthor(BookAuthor bookAuthor)
        => context.BookAuthors.Remove(bookAuthor);
}
