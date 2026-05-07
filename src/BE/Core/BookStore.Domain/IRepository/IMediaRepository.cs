using BookStore.Domain.Entities;
using BookStore.Domain.Enums;

namespace BookStore.Domain.IRepository;

public interface IMediaRepository
{
    Task<Media?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<Media>> GetListAsync(
        Guid userId, bool isAdmin,
        string? module, MediaType? type,
        DateTime? before, int limit,
        CancellationToken ct = default);
    void Add(Media media);
    void Remove(Media media);
}
