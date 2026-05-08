using BookStore.Application.Media.Commands;
using BookStore.Application.Media.DTOs;
using BookStore.Application.Media.IService;
using BookStore.Domain.Enums;
using BookStore.Domain.IRepository;
using BookStore.Shared.Results;
using MediaEntity = BookStore.Domain.Entities.Media;
using Microsoft.Extensions.Options;

namespace BookStore.Application.Media.Services;

public sealed class MediaService(
    IMediaRepository mediaRepo,
    IUnitOfWork unitOfWork,
    IMinioStorageService storageService,
    IOptions<MinioSettings> minioOptions) : IMediaService
{
    private readonly MinioSettings _settings = minioOptions.Value;

    public async Task<Result<MediaDto>> UploadAsync(UploadMediaCommand cmd, CancellationToken ct = default)
    {
        var file = cmd.File;

        if (!_settings.AllowedMimeTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
            return MediaErrors.InvalidFileType;

        if (file.Length > _settings.MaxFileSizeBytes)
            return MediaErrors.FileTooLarge;

        if (!_settings.Buckets.TryGetValue(Capitalize(cmd.Module), out var bucketName))
            bucketName = cmd.Module;

        var ext       = Path.GetExtension(file.FileName).TrimStart('.');
        var datePath  = DateTime.UtcNow.ToString("yyyy/MM/dd");
        var objectKey = $"{cmd.Module}/{datePath}/{Guid.NewGuid()}.{ext}";

        try
        {
            await using var stream = file.OpenReadStream();
            await storageService.UploadAsync(bucketName, objectKey, stream, file.ContentType, file.Length, ct);
        }
        catch
        {
            return MediaErrors.UploadFailed;
        }

        var mediaType = ResolveMediaType(file.ContentType);
        var media = MediaEntity.Create(
            objectKey, null, bucketName, cmd.Module,
            file.FileName, file.ContentType, file.Length,
            null, null, mediaType, cmd.UploadedBy);

        mediaRepo.Add(media);
        await unitOfWork.SaveChangesAsync(ct);

        var url = await storageService.GeneratePresignedUrlAsync(
            bucketName, objectKey, _settings.PresignedUrlExpirySeconds);

        return MapToDto(media, url, null);
    }

    public async Task<Result> DeleteAsync(
        Guid mediaId, Guid requestingUserId, bool isAdmin, CancellationToken ct = default)
    {
        var media = await mediaRepo.GetByIdAsync(mediaId, ct);
        if (media is null)
            return Result.Failure(MediaErrors.NotFound(mediaId));

        var canDelete = media.CanDelete(requestingUserId, isAdmin);
        if (canDelete.IsFailure)
            return canDelete;

        try
        {
            await storageService.DeleteAsync(media.BucketName, media.ObjectKey, ct);

            if (media.ThumbnailKey is not null)
                await storageService.DeleteAsync(media.BucketName, media.ThumbnailKey, ct);
        }
        catch
        {
            return Result.Failure(MediaErrors.DeleteFailed);
        }

        mediaRepo.Remove(media);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    private static MediaType ResolveMediaType(string mimeType) => mimeType switch
    {
        var m when m.StartsWith("image/") => MediaType.Image,
        var m when m.StartsWith("video/") => MediaType.Video,
        "application/pdf"                 => MediaType.Document,
        _                                 => MediaType.Other
    };

    private static string Capitalize(string s)
        => s.Length == 0 ? s : char.ToUpperInvariant(s[0]) + s[1..];

    internal static MediaDto MapToDto(MediaEntity media, string url, string? thumbnailUrl) => new()
    {
        Id           = media.Id,
        ObjectKey    = media.ObjectKey,
        Url          = url,
        ThumbnailUrl = thumbnailUrl,
        Type         = media.Type.ToString().ToLowerInvariant(),
        MimeType     = media.MimeType,
        Size         = media.Size,
        Width        = media.Width,
        Height       = media.Height,
        CreatedAt    = media.CreatedAt,
        UploadedBy   = media.UploadedBy
    };
}
