namespace BookStore.Application.Media.DTOs;

public sealed record MediaDto
{
    public Guid     Id           { get; init; }
    public string   ObjectKey    { get; init; } = string.Empty;
    public string   Url          { get; init; } = string.Empty;
    public string?  ThumbnailUrl { get; init; }
    public string   Type         { get; init; } = string.Empty;
    public string   MimeType     { get; init; } = string.Empty;
    public long     Size         { get; init; }
    public int?     Width        { get; init; }
    public int?     Height       { get; init; }
    public DateTime CreatedAt    { get; init; }
    public Guid     UploadedBy   { get; init; }
}
