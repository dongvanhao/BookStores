using BookStore.Domain.Common;
using BookStore.Domain.Enums;
using BookStore.Shared.Results;

namespace BookStore.Domain.Entities;

public class Media : BaseEntity
{
    public string    ObjectKey        { get; private set; } = string.Empty;
    public string?   ThumbnailKey     { get; private set; }
    public string    BucketName       { get; private set; } = string.Empty;
    public string    Module           { get; private set; } = string.Empty;
    public string    OriginalFileName { get; private set; } = string.Empty;
    public string    MimeType         { get; private set; } = string.Empty;
    public long      Size             { get; private set; }
    public int?      Width            { get; private set; }
    public int?      Height           { get; private set; }
    public MediaType Type             { get; private set; }
    public Guid      UploadedBy       { get; private set; }

    private Media() { }

    public static Media Create(
        string objectKey,
        string? thumbnailKey,
        string bucketName,
        string module,
        string originalFileName,
        string mimeType,
        long size,
        int? width,
        int? height,
        MediaType type,
        Guid uploadedBy)
    {
        return new Media
        {
            Id               = Guid.NewGuid(),
            ObjectKey        = objectKey,
            ThumbnailKey     = thumbnailKey,
            BucketName       = bucketName,
            Module           = module,
            OriginalFileName = originalFileName,
            MimeType         = mimeType,
            Size             = size,
            Width            = width,
            Height           = height,
            Type             = type,
            UploadedBy       = uploadedBy,
            CreatedAt        = DateTime.UtcNow,
            UpdatedAt        = DateTime.UtcNow
        };
    }

    public Result CanDelete(Guid requestingUserId, bool isAdmin)
    {
        if (isAdmin || UploadedBy == requestingUserId)
            return Result.Success();

        return Result.Failure(Error.Forbidden("Media.Forbidden", "You are not allowed to delete this media."));
    }
}
