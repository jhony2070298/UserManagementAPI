using System.Security.Cryptography;
using System.Text;
using System.Net.Http.Headers;

namespace UserManagementAPI.Middleware;

internal sealed class BearerTokenAuthenticationMiddleware(
    RequestDelegate next,
    ILogger<BearerTokenAuthenticationMiddleware> logger,
    IConfiguration configuration)
{
    private readonly byte[] _configuredToken = Encoding.UTF8.GetBytes(
        configuration["Authentication:BearerToken"]!);

    public async Task InvokeAsync(HttpContext context)
    {
        var authorization = context.Request.Headers.Authorization.ToString();
        var token = AuthenticationHeaderValue.TryParse(authorization, out var header)
            && header.Scheme.Equals("Bearer", StringComparison.OrdinalIgnoreCase)
            ? header.Parameter
            : null;

        var isValid = token is not null
            && CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(token),
                _configuredToken);

        if (!isValid)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.Headers.WWWAuthenticate = "Bearer";
            logger.LogWarning("HTTP {Method} {Path} responded {StatusCode}",
                context.Request.Method, context.Request.Path, context.Response.StatusCode);
            await context.Response.WriteAsJsonAsync(
                new { error = "Unauthorized." },
                context.RequestAborted);
            return;
        }

        await next(context);
    }
}
