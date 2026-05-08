using BookStore.Application.Media.DTOs;
using BookStore.Application.Media.IService;
using BookStore.Application.Media.Queries;
using BookStore.Domain.IRepository;
using BookStore.Shared.Results;
using Microsoft.Extensions.Options;

namespace BookStore.Application.Media.Services;

public sealed class MediaQueryService(
    IMediaRepository mediaRepo,
    IMinioStorageService storageService,
    IOptions<MinioSettings> minioOptions) : IMediaQueryService
{
    private readonly MinioSettings _settings = minioOptions.Value;

    public async Task<Result<MediaDto>> GetByIdAsync(
        Guid id, Guid userId, bool isAdmin, CancellationToken ct = default)
    {
        var media = await mediaRepo.GetByIdAsync(id, ct);
        if (media is null)
            return MediaErrors.NotFound(id);

        if (!isAdmin && media.UploadedBy != userId)
            return MediaErrors.Forbidden;

        var url = await storageService.GeneratePresignedUrlAsync(
            media.BucketName, media.ObjectKey, _settings.PresignedUrlExpirySeconds);

        string? thumbnailUrl = null;
        if (media.ThumbnailKey is not null)
            thumbnailUrl = await storageService.GeneratePresignedUrlAsync(
                media.BucketName, media.ThumbnailKey, _settings.PresignedUrlExpirySeconds);

        return MediaService.MapToDto(media, url, thumbnailUrl);
    }

    public async Task<Result<MediaListResponse>> GetListAsync(
        GetMediaListQuery query, Guid userId, bool isAdmin, CancellationToken ct = default)
    {
        var items = await mediaRepo.GetListAsync(
            userId, isAdmin,
            query.Module, query.Type, query.Before,
            query.Limit, ct);

        var hasMore = items.Count > query.Limit;
        if (hasMore)
            items = items.Take(query.Limit).ToList();

        var urlTasks = items.Select(m => storageService.GeneratePresignedUrlAsync(
            m.BucketName, m.ObjectKey, _settings.PresignedUrlExpirySeconds));

        var urls = await Task.WhenAll(urlTasks);

        var dtos = items.Select((m, i) => new MediaSummaryDto
        {
            Id           = m.Id,
            Url          = urls[i],
            ThumbnailUrl = null,
            Type         = m.Type.ToString().ToLowerInvariant(),
            CreatedAt    = m.CreatedAt
        }).ToList();

        return new MediaListResponse
        {
            Data = dtos,
            Meta = new MediaCursorMeta
            {
                NextCursor = hasMore ? items.Last().CreatedAt : null,
                Limit      = query.Limit,
                HasMore    = hasMore
            }
        };
    }
}
