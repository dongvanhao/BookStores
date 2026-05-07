using BookStore.Shared.Results;

namespace BookStore.Application.Media;

public static class MediaErrors
{
    public static Error NotFound(Guid id)
        => Error.NotFound("Media.NotFound", $"Media '{id}' was not found.");

    public static readonly Error Forbidden
        = Error.Forbidden("Media.Forbidden", "You are not allowed to perform this action on this media.");

    public static readonly Error InvalidFileType
        = Error.Validation("Media.InvalidFileType", "File type is not allowed.");

    public static readonly Error FileTooLarge
        = Error.Validation("Media.FileTooLarge", "File exceeds the maximum allowed size.");

    public static readonly Error UploadFailed
        = Error.Failure("Media.UploadFailed", "Failed to upload file to storage.");

    public static readonly Error DeleteFailed
        = Error.Failure("Media.DeleteFailed", "Failed to delete file from storage.");
}
