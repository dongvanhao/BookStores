using BookStore.Domain.Entities;
using BookStore.Domain.IRepository;
using BookStore.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Infrastructure.Repository;

public class CategoryRepository(AppDbContext context) : ICategoryRepository
{
    public async Task<Category?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await context.Categories
            .Include(c => c.Parent)
            .Include(c => c.Children)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<Category?> GetByIdWithChildrenAsync(Guid id, CancellationToken ct = default)
    {
        // Load all so EF Core fix-up populates the full hierarchy
        var all = await context.Categories.ToListAsync(ct);
        return all.FirstOrDefault(c => c.Id == id);
    }

    public async Task<List<Category>> GetAllWithChildrenAsync(CancellationToken ct = default)
        // EF Core identity fix-up automatically wires up Children when all entities
        // are loaded into the same context instance — no recursive Include needed
        => await context.Categories.ToListAsync(ct);

    public IQueryable<Category> GetQueryable()
        => context.Categories
            .Include(c => c.Parent)
            .Include(c => c.Children)
            .AsQueryable();

    public async Task<bool> HasChildrenAsync(Guid id, CancellationToken ct = default)
        => await context.Categories.AnyAsync(c => c.ParentId == id, ct);

    public async Task<bool> HasBooksAsync(Guid id, CancellationToken ct = default)
        => await context.Books.AnyAsync(b => b.CategoryId == id, ct);

    public async Task<List<Guid>> GetDescendantIdsAsync(Guid id, CancellationToken ct = default)
    {
        var all = await context.Categories
            .Select(c => new { c.Id, c.ParentId })
            .ToListAsync(ct);

        var result = new List<Guid>();
        var queue = new Queue<Guid>([id]);

        while (queue.TryDequeue(out var current))
        {
            foreach (var child in all.Where(c => c.ParentId == current))
            {
                result.Add(child.Id);
                queue.Enqueue(child.Id);
            }
        }

        return result;
    }

    public void Add(Category category) => context.Categories.Add(category);

    public void Remove(Category category) => context.Categories.Remove(category);
}
