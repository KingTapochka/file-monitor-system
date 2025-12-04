using FileMonitorService.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FileMonitorService.Services;

/// <summary>
/// Кэш для хранения информации об открытых файлах
/// </summary>
public class FileMonitorCache : IFileMonitorCache
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<FileMonitorCache> _logger;
    private readonly TimeSpan _cacheExpiration;
    private const string CACHE_KEY = "open_files";
    private readonly object _lock = new();

    public FileMonitorCache(
        IMemoryCache cache, 
        ILogger<FileMonitorCache> logger,
        IConfiguration configuration)
    {
        _cache = cache;
        _logger = logger;
        
        // Чтение времени жизни кэша из конфигурации
        var expirationMinutes = configuration.GetValue<int>("FileMonitor:CacheExpirationMinutes", 5);
        _cacheExpiration = TimeSpan.FromMinutes(expirationMinutes);
        
        _logger.LogInformation("Время жизни кэша: {Expiration} минут", expirationMinutes);
    }

    public void UpdateCache(List<FileUserInfo> openFiles)
    {
        lock (_lock)
        {
            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(_cacheExpiration);

            _cache.Set(CACHE_KEY, openFiles, cacheOptions);
            _logger.LogDebug("Кэш обновлен: {Count} файлов", openFiles.Count);
        }
    }

    public FileUsersResponse? GetFileUsers(string filePath)
    {
        var openFiles = GetCachedFiles();
        if (openFiles == null || openFiles.Count == 0)
            return null;

        var normalizedPath = NormalizePath(filePath);
        var users = openFiles
            .Where(f => NormalizePath(f.FilePath).Equals(normalizedPath, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (users.Count == 0)
            return null;

        return new FileUsersResponse
        {
            FilePath = filePath,
            Users = users,
            LastUpdated = DateTime.Now
        };
    }

    public List<ActiveFileInfo> GetActiveFiles()
    {
        var openFiles = GetCachedFiles();
        if (openFiles == null || openFiles.Count == 0)
            return new List<ActiveFileInfo>();

        return openFiles
            .GroupBy(f => f.FilePath, StringComparer.OrdinalIgnoreCase)
            .Select(g => new ActiveFileInfo
            {
                FilePath = g.Key,
                UserCount = g.Count(),
                LastAccess = g.Max(f => f.OpenedAt)
            })
            .OrderByDescending(f => f.UserCount)
            .ToList();
    }

    public List<FileUserInfo> GetUserFiles(string userName)
    {
        var openFiles = GetCachedFiles();
        if (openFiles == null || openFiles.Count == 0)
            return new List<FileUserInfo>();

        return openFiles
            .Where(f => f.UserName.Equals(userName, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public void ClearCache()
    {
        lock (_lock)
        {
            _cache.Remove(CACHE_KEY);
            _logger.LogInformation("Кэш очищен");
        }
    }

    private List<FileUserInfo>? GetCachedFiles()
    {
        return _cache.Get<List<FileUserInfo>>(CACHE_KEY);
    }

    private string NormalizePath(string path)
    {
        if (string.IsNullOrEmpty(path))
            return string.Empty;

        return path.Replace('/', '\\').TrimEnd('\\').ToLowerInvariant();
    }
}
