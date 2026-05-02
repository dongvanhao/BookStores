using BookStore.Shared.Responses;

namespace BookStore.API.Middleware;

public sealed class ExceptionMiddleware
{
    private readonly RequestDelegate              _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(
        RequestDelegate next,
        ILogger<ExceptionMiddleware> logger)
    {
        _next   = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unhandled exception at {Path}: {Message}",
                context.Request.Path,
                ex.Message);

            await HandleAsync(context, ex);
        }
    }

    private static async Task HandleAsync(HttpContext context, Exception exception)
    {
        var (statusCode, message, errorCode) = exception switch
        {
            OperationCanceledException  => (499, "Request was cancelled.",        "Cancelled"),
            UnauthorizedAccessException => (401, "Unauthorized.",                 "Unauthorized"),
            _                           => (500, "An unexpected error occurred.", "InternalServerError")
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode  = statusCode;

        var response = ApiResponse.Fail(message, errorCode);
        await context.Response.WriteAsJsonAsync(response);
    }
}