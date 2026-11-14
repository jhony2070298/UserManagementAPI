namespace UserManagementAPI.Middleware;

internal sealed class RequestResponseLoggingMiddleware(
    RequestDelegate next,
    ILogger<RequestResponseLoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            logger.LogInformation("HTTP {Method} {Path} responded {StatusCode}",
                context.Request.Method, context.Request.Path, context.Response.StatusCode);
            return Task.CompletedTask;
        });

        await next(context);
    }
}
