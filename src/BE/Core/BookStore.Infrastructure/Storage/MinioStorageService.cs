using BookStore.Application.Media.IService;
using Minio;
using Minio.DataModel.Args;

namespace BookStore.Infrastructure.Storage;

public sealed class MinioStorageService(IMinioClient minioClient) : IMinioStorageService
{
    public async Task UploadAsync(
        string bucketName, string objectKey,
        Stream stream, string contentType, long size,
        CancellationToken ct)
    {
        var args = new PutObjectArgs()
            .WithBucket(bucketName)
            .WithObject(objectKey)
            .WithStreamData(stream)
            .WithObjectSize(size)
            .WithContentType(contentType);

        await minioClient.PutObjectAsync(args, ct);
    }

    public async Task DeleteAsync(string bucketName, string objectKey, CancellationToken ct)
    {
        var args = new RemoveObjectArgs()
            .WithBucket(bucketName)
            .WithObject(objectKey);

        await minioClient.RemoveObjectAsync(args, ct);
    }

    public async Task<string> GeneratePresignedUrlAsync(string bucketName, string objectKey, int expirySeconds)
    {
        var args = new PresignedGetObjectArgs()
            .WithBucket(bucketName)
            .WithObject(objectKey)
            .WithExpiry(expirySeconds);

        return await minioClient.PresignedGetObjectAsync(args);
    }

    public async Task EnsureBucketsAsync(IEnumerable<string> bucketNames, CancellationToken ct)
    {
        foreach (var bucketName in bucketNames)
        {
            var existsArgs = new BucketExistsArgs().WithBucket(bucketName);
            var exists = await minioClient.BucketExistsAsync(existsArgs, ct);
            if (!exists)
            {
                var makeArgs = new MakeBucketArgs().WithBucket(bucketName);
                await minioClient.MakeBucketAsync(makeArgs, ct);
            }
        }
    }
}
