using BookStore.Domain.Entities;
using BookStore.Domain.IRepository;
using BookStore.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Infrastructure.Repository;

public class AuthorRepository(AppDbContext context) : IAuthorRepository
{
    public async Task<Author?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await context.Authors.FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task<Author?> GetByIdWithBooksAsync(Guid id, CancellationToken ct = default)
        => await context.Authors
            .Include(a => a.BookAuthors)
                .ThenInclude(ba => ba.Book)
            .FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task<bool> ExistsByFullNameAsync(string fullName, CancellationToken ct = default)
        => await context.Authors.AnyAsync(a => a.FullName == fullName, ct);

    public async Task<bool> HasBooksAsync(Guid id, CancellationToken ct = default)
        => await context.BookAuthors.AnyAsync(ba => ba.AuthorId == id, ct);

    public IQueryable<Author> GetQueryable()
        => context.Authors.AsQueryable();

    public void Add(Author author) => context.Authors.Add(author);

    public void Remove(Author author) => context.Authors.Remove(author);
}
