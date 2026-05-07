using BookStore.Application.Media.Commands;
using BookStore.Application.Media.DTOs;
using BookStore.Shared.Results;

namespace BookStore.Application.Media.IService;

public interface IMediaService
{
    Task<Result<MediaDto>> UploadAsync(UploadMediaCommand cmd, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid mediaId, Guid requestingUserId, bool isAdmin, CancellationToken ct = default);
}
