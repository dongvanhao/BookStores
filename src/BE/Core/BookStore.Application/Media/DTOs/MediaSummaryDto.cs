namespace BookStore.Application.Media.DTOs;

public sealed record MediaSummaryDto
{
    public Guid     Id           { get; init; }
    public string   Url          { get; init; } = string.Empty;
    public string?  ThumbnailUrl { get; init; }
    public string   Type         { get; init; } = string.Empty;
    public DateTime CreatedAt    { get; init; }
}
