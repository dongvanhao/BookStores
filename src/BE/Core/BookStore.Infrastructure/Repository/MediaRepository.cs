using BookStore.Domain.Entities;
using BookStore.Domain.Enums;
using BookStore.Domain.IRepository;
using BookStore.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Infrastructure.Repository;

public class MediaRepository(AppDbContext context) : IMediaRepository
{
    public async Task<Media?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await context.Media.FirstOrDefaultAsync(m => m.Id == id, ct);

    public async Task<List<Media>> GetListAsync(
        Guid userId, bool isAdmin,
        string? module, MediaType? type,
        DateTime? before, int limit,
        CancellationToken ct = default)
    {
        var query = context.Media.AsNoTracking();

        if (!isAdmin)
            query = query.Where(m => m.UploadedBy == userId);

        if (module is not null)
            query = query.Where(m => m.Module == module);

        if (type is not null)
            query = query.Where(m => m.Type == type);

        if (before is not null)
            query = query.Where(m => m.CreatedAt < before);

        return await query
            .OrderByDescending(m => m.CreatedAt)
            .Take(limit + 1)
            .ToListAsync(ct);
    }

    public void Add(Media media) => context.Media.Add(media);

    public void Remove(Media media) => context.Media.Remove(media);
}
