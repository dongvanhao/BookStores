namespace BookStore.Application.Media.DTOs;

public sealed record MediaListResponse
{
    public IReadOnlyList<MediaDto> Data { get; init; } = [];
    public MediaCursorMeta         Meta { get; init; } = null!;
}

public sealed record MediaCursorMeta
{
    public DateTime? NextCursor { get; init; }
    public int       Limit      { get; init; }
    public bool      HasMore    { get; init; }
}
