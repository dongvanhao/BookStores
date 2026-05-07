namespace BookStore.Application.Media.IService;

public interface IMinioStorageService
{
    Task UploadAsync(string bucketName, string objectKey, Stream stream, string contentType, long size, CancellationToken ct);
    Task DeleteAsync(string bucketName, string objectKey, CancellationToken ct);
    Task<string> GeneratePresignedUrlAsync(string bucketName, string objectKey, int expirySeconds);
    Task EnsureBucketsAsync(IEnumerable<string> bucketNames, CancellationToken ct);
}
