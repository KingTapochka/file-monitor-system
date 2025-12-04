using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FileMonitorService.Middleware;

/// <summary>
/// Middleware для проверки API ключа в заголовке запроса
/// </summary>
public class ApiKeyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiKeyMiddleware> _logger;
    private readonly string? _apiKey;
    private readonly bool _enabled;

    private const string API_KEY_HEADER = "X-API-Key";

    public ApiKeyMiddleware(
        RequestDelegate next,
        ILogger<ApiKeyMiddleware> logger,
        IConfiguration configuration)
    {
        _next = next;
        _logger = logger;
        _apiKey = configuration["Security:ApiKey"];
        _enabled = !string.IsNullOrEmpty(_apiKey);

        if (_enabled)
        {
            _logger.LogInformation("API Key authentication enabled");
        }
        else
        {
            _logger.LogWarning("API Key authentication DISABLED - set Security:ApiKey in appsettings.json");
        }
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Если API ключ не настроен - пропускаем проверку
        if (!_enabled)
        {
            await _next(context);
            return;
        }

        // Пропускаем health check без аутентификации
        if (context.Request.Path.StartsWithSegments("/api/files/health"))
        {
            await _next(context);
            return;
        }

        // Проверяем заголовок X-API-Key
        if (!context.Request.Headers.TryGetValue(API_KEY_HEADER, out var providedKey))
        {
            _logger.LogWarning("API request without key from {IP}", 
                context.Connection.RemoteIpAddress);
            
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new 
            { 
                error = "API key required",
                message = "Provide X-API-Key header"
            });
            return;
        }

        if (!string.Equals(providedKey, _apiKey, StringComparison.Ordinal))
        {
            _logger.LogWarning("Invalid API key from {IP}", 
                context.Connection.RemoteIpAddress);
            
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new 
            { 
                error = "Invalid API key"
            });
            return;
        }

        await _next(context);
    }
}

/// <summary>
/// Extension для регистрации middleware
/// </summary>
public static class ApiKeyMiddlewareExtensions
{
    public static IApplicationBuilder UseApiKeyAuthentication(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ApiKeyMiddleware>();
    }
}
