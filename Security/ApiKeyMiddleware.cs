using System.Net;

namespace MiddlewareInfocert.Security;

public class ApiKeyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string _configuredApiKey;

    public ApiKeyMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        _configuredApiKey = configuration["Security:ApiKey"]
            ?? throw new InvalidOperationException("API Key non configurata");
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue("X-API-KEY", out var extractedApiKey))
        {
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            await context.Response.WriteAsync("API Key mancante.");
            return;
        }

        if (!_configuredApiKey.Equals(extractedApiKey, StringComparison.OrdinalIgnoreCase))
        {
            context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            await context.Response.WriteAsync("API Key non valida.");
            return;
        }

        await _next(context);
    }
}
