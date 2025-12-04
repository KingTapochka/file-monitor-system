using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FileMonitorService.Services;

/// <summary>
/// Сервис для преобразования путей между UNC и локальными
/// </summary>
public interface IPathMappingService
{
    /// <summary>
    /// Преобразует UNC путь (\\server\share\file) в локальный (D:\folder\file)
    /// </summary>
    string UncToLocal(string uncPath);
    
    /// <summary>
    /// Преобразует локальный путь (D:\folder\file) в UNC (\\server\share\file)
    /// </summary>
    string LocalToUnc(string localPath);
    
    /// <summary>
    /// Получает все возможные варианты пути (UNC и локальный)
    /// </summary>
    List<string> GetAllPathVariants(string path);
    
    /// <summary>
    /// Сравнивает два пути (могут быть в разных форматах)
    /// </summary>
    bool PathsMatch(string path1, string path2);
}

public class PathMappingService : IPathMappingService
{
    private readonly ILogger<PathMappingService> _logger;
    private readonly List<ShareMapping> _shareMappings;
    private readonly string _serverName;

    public PathMappingService(IConfiguration configuration, ILogger<PathMappingService> logger)
    {
        _logger = logger;
        _shareMappings = new List<ShareMapping>();
        
        // Загрузка маппингов из конфигурации
        var mappingsSection = configuration.GetSection("ShareMappings");
        if (mappingsSection.Exists())
        {
            foreach (var item in mappingsSection.GetChildren())
            {
                var shareName = item["ShareName"];
                var localPath = item["LocalPath"];
                
                if (!string.IsNullOrEmpty(shareName) && !string.IsNullOrEmpty(localPath))
                {
                    _shareMappings.Add(new ShareMapping
                    {
                        ShareName = shareName.ToLowerInvariant(),
                        LocalPath = NormalizePath(localPath)
                    });
                    _logger.LogInformation("Загружен маппинг: {ShareName} -> {LocalPath}", shareName, localPath);
                }
            }
        }
        
        // Имя сервера (если не указано - получаем автоматически)
        _serverName = configuration["ServerSettings:ServerName"] ?? Environment.MachineName;
        _logger.LogInformation("Имя сервера: {ServerName}", _serverName);
        
        // Автоматическое обнаружение шар через SMB
        LoadSmbShares();
    }

    private void LoadSmbShares()
    {
        try
        {
            // Получаем список SMB шар через PowerShell
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = "-NoProfile -Command \"Get-SmbShare | Select-Object Name, Path | ConvertTo-Json\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = System.Diagnostics.Process.Start(psi);
            if (process == null) return;
            
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            if (string.IsNullOrWhiteSpace(output)) return;

            using var doc = System.Text.Json.JsonDocument.Parse(output);
            var root = doc.RootElement;

            void ProcessShare(System.Text.Json.JsonElement element)
            {
                var name = element.TryGetProperty("Name", out var nameProp) ? nameProp.GetString() : null;
                var path = element.TryGetProperty("Path", out var pathProp) ? pathProp.GetString() : null;
                
                if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(path)) return;
                if (name.EndsWith("$")) return; // Пропускаем скрытые шары
                
                var normalizedName = name.ToLowerInvariant();
                
                // Проверяем, нет ли уже такого маппинга
                if (!_shareMappings.Any(m => m.ShareName == normalizedName))
                {
                    _shareMappings.Add(new ShareMapping
                    {
                        ShareName = normalizedName,
                        LocalPath = NormalizePath(path)
                    });
                    _logger.LogInformation("Автоматически обнаружена шара: {ShareName} -> {Path}", name, path);
                }
            }

            if (root.ValueKind == System.Text.Json.JsonValueKind.Array)
            {
                foreach (var item in root.EnumerateArray())
                {
                    ProcessShare(item);
                }
            }
            else if (root.ValueKind == System.Text.Json.JsonValueKind.Object)
            {
                ProcessShare(root);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Не удалось получить список SMB шар");
        }
    }

    public string UncToLocal(string uncPath)
    {
        if (string.IsNullOrEmpty(uncPath)) return uncPath;
        
        // Если это уже локальный путь (C:\...), возвращаем как есть
        if (uncPath.Length >= 2 && uncPath[1] == ':')
            return NormalizePath(uncPath);

        // Парсим UNC путь: \\server\share\path\to\file
        if (!uncPath.StartsWith(@"\\")) return uncPath;

        var parts = uncPath.Substring(2).Split(new[] { '\\' }, 3);
        if (parts.Length < 2) return uncPath;

        var shareName = parts[1].ToLowerInvariant();
        var relativePath = parts.Length > 2 ? parts[2] : "";

        // Ищем маппинг для шары
        var mapping = _shareMappings.FirstOrDefault(m => m.ShareName == shareName);
        if (mapping == null)
        {
            _logger.LogDebug("Маппинг для шары '{ShareName}' не найден", shareName);
            return uncPath;
        }

        var localPath = string.IsNullOrEmpty(relativePath) 
            ? mapping.LocalPath 
            : Path.Combine(mapping.LocalPath, relativePath);
            
        return NormalizePath(localPath);
    }

    public string LocalToUnc(string localPath)
    {
        if (string.IsNullOrEmpty(localPath)) return localPath;
        
        var normalizedLocal = NormalizePath(localPath);
        
        // Ищем маппинг, где локальный путь начинается с пути шары
        foreach (var mapping in _shareMappings)
        {
            if (normalizedLocal.StartsWith(mapping.LocalPath, StringComparison.OrdinalIgnoreCase))
            {
                var relativePath = normalizedLocal.Substring(mapping.LocalPath.Length).TrimStart('\\');
                var uncPath = $@"\\{_serverName}\{mapping.ShareName}";
                if (!string.IsNullOrEmpty(relativePath))
                {
                    uncPath = Path.Combine(uncPath, relativePath);
                }
                return uncPath;
            }
        }
        
        return localPath;
    }

    public List<string> GetAllPathVariants(string path)
    {
        var variants = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        
        var normalized = NormalizePath(path);
        variants.Add(normalized);
        
        // Если это UNC путь, добавляем локальный вариант
        if (path.StartsWith(@"\\"))
        {
            var localPath = UncToLocal(path);
            if (!string.IsNullOrEmpty(localPath) && localPath != path)
            {
                variants.Add(NormalizePath(localPath));
            }
        }
        else
        {
            // Если это локальный путь, добавляем UNC вариант
            var uncPath = LocalToUnc(path);
            if (!string.IsNullOrEmpty(uncPath) && uncPath != path)
            {
                variants.Add(NormalizePath(uncPath));
            }
        }
        
        return variants.ToList();
    }

    public bool PathsMatch(string path1, string path2)
    {
        if (string.IsNullOrEmpty(path1) || string.IsNullOrEmpty(path2))
            return false;

        var normalized1 = NormalizePath(path1);
        var normalized2 = NormalizePath(path2);

        // Прямое сравнение
        if (normalized1.Equals(normalized2, StringComparison.OrdinalIgnoreCase))
            return true;

        // Получаем все варианты и сравниваем
        var variants1 = GetAllPathVariants(path1);
        var variants2 = GetAllPathVariants(path2);

        return variants1.Any(v1 => variants2.Any(v2 => 
            v1.Equals(v2, StringComparison.OrdinalIgnoreCase)));
    }

    private string NormalizePath(string path)
    {
        if (string.IsNullOrEmpty(path)) return string.Empty;
        return path.Replace('/', '\\').TrimEnd('\\').ToLowerInvariant();
    }

    private class ShareMapping
    {
        public string ShareName { get; set; } = "";
        public string LocalPath { get; set; } = "";
    }
}
