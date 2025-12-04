using System.Collections.Concurrent;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FileMonitorService.Middleware;

/// <summary>
/// Middleware для ограничения количества запросов (Rate Limiting)
/// </summary>
public class RateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitMiddleware> _logger;
    private readonly int _maxRequests;
    private readonly TimeSpan _window;
    private readonly bool _enabled;
    
    private readonly ConcurrentDictionary<string, RateLimitInfo> _clients = new();

    public RateLimitMiddleware(
        RequestDelegate next,
        ILogger<RateLimitMiddleware> logger,
        IConfiguration configuration)
    {
        _next = next;
        _logger = logger;
        
        _maxRequests = configuration.GetValue<int>("Security:RateLimit:MaxRequests", 0);
        var windowSeconds = configuration.GetValue<int>("Security:RateLimit:WindowSeconds", 60);
        _window = TimeSpan.FromSeconds(windowSeconds);
        
        _enabled = _maxRequests > 0;

        if (_enabled)
        {
            _logger.LogInformation("Rate limiting enabled: {Max} requests per {Window}s", 
                _maxRequests, windowSeconds);
        }
        else
        {
            _logger.LogWarning("Rate limiting DISABLED - set Security:RateLimit:MaxRequests in appsettings.json");
        }
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!_enabled)
        {
            await _next(context);
            return;
        }

        var clientId = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var now = DateTime.UtcNow;

        // Очистка старых записей (раз в 100 запросов)
        if (_clients.Count > 1000)
        {
            CleanupOldEntries(now);
        }

        var info = _clients.GetOrAdd(clientId, _ => new RateLimitInfo { WindowStart = now });

        lock (info)
        {
            // Если окно истекло - сбрасываем счётчик
            if (now - info.WindowStart > _window)
            {
                info.WindowStart = now;
                info.RequestCount = 0;
            }

            info.RequestCount++;

            if (info.RequestCount > _maxRequests)
            {
                _logger.LogWarning("Rate limit exceeded for {ClientId}: {Count} requests", 
                    clientId, info.RequestCount);

                context.Response.StatusCode = 429; // Too Many Requests
                context.Response.Headers["Retry-After"] = ((int)(_window - (now - info.WindowStart)).TotalSeconds).ToString();
                
                context.Response.WriteAsJsonAsync(new
                {
                    error = "Too many requests",
                    message = $"Rate limit: {_maxRequests} requests per {_window.TotalSeconds}s",
                    retryAfter = (int)(_window - (now - info.WindowStart)).TotalSeconds
                }).Wait();
                
                return;
            }
        }

        await _next(context);
    }

    private void CleanupOldEntries(DateTime now)
    {
        var keysToRemove = _clients
            .Where(kvp => now - kvp.Value.WindowStart > _window * 2)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in keysToRemove)
        {
            _clients.TryRemove(key, out _);
        }
    }

    private class RateLimitInfo
    {
        public DateTime WindowStart { get; set; }
        public int RequestCount { get; set; }
    }
}

public static class RateLimitMiddlewareExtensions
{
    public static IApplicationBuilder UseRateLimiting(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RateLimitMiddleware>();
    }
}
