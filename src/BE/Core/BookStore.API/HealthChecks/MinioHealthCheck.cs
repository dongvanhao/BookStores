using Microsoft.Extensions.Diagnostics.HealthChecks;
using Minio;

namespace BookStore.API.HealthChecks;

public class MinioHealthCheck(IServiceScopeFactory scopeFactory) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var minioClient = scope.ServiceProvider.GetService<IMinioClient>();

            if (minioClient is null)
                return HealthCheckResult.Degraded("MinIO client chưa được đăng ký.");

            await minioClient.ListBucketsAsync(cancellationToken);
            return HealthCheckResult.Healthy("MinIO is reachable.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("MinIO connection failed.", ex);
        }
    }
}
