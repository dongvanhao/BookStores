using BookStore.Application.Media.DTOs;
using BookStore.Application.Media.Queries;
using BookStore.Shared.Results;

namespace BookStore.Application.Media.IService;

public interface IMediaQueryService
{
    Task<Result<MediaDto>> GetByIdAsync(Guid id, Guid userId, bool isAdmin, CancellationToken ct = default);

    Task<Result<MediaListResponse>> GetListAsync(GetMediaListQuery query, Guid userId, bool isAdmin, CancellationToken ct = default);
}
