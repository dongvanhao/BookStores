using BookStore.API.HealthChecks;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text.Json;

namespace BookStore.API.Extensions;

public static class HealthCheckExtensions
{
    public static IServiceCollection AddCustomHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>("database", tags: ["database"])
            .AddCheck<MinioHealthCheck>("minio",       tags: ["minio"]);

        return services;
    }

    public static WebApplication MapHealthCheckEndpoints(this WebApplication app)
    {
        // GET /health — backend alive (no external dependency checks)
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            Predicate      = _ => false,
            ResponseWriter = WriteResponse
        });

        // GET /health/database — SQL Server connectivity
        app.MapHealthChecks("/health/database", new HealthCheckOptions
        {
            Predicate      = r => r.Tags.Contains("database"),
            ResponseWriter = WriteResponse
        });

        // GET /health/minio — MinIO connectivity
        app.MapHealthChecks("/health/minio", new HealthCheckOptions
        {
            Predicate      = r => r.Tags.Contains("minio"),
            ResponseWriter = WriteResponse
        });

        return app;
    }

    private static Task WriteResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json; charset=utf-8";

        var payload = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name        = e.Key,
                status      = e.Value.Status.ToString(),
                description = e.Value.Description,
                duration    = e.Value.Duration.ToString()
            }),
            totalDuration = report.TotalDuration.ToString()
        };

        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            WriteIndented       = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        return context.Response.WriteAsync(json);
    }
}
