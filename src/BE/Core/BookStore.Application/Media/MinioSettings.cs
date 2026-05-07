namespace BookStore.Application.Media;

public sealed class MinioSettings
{
    public string Endpoint                    { get; init; } = string.Empty;
    public string AccessKey                   { get; init; } = string.Empty;
    public string SecretKey                   { get; init; } = string.Empty;
    public bool   UseSsl                      { get; init; } = false;
    public Dictionary<string, string> Buckets { get; init; } = new();
    public int    PresignedUrlExpirySeconds   { get; init; } = 3600;
    public List<string> AllowedMimeTypes      { get; init; } = [];
    public long   MaxFileSizeBytes            { get; init; } = 10_485_760;
}
