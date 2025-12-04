using System.Management.Automation;
using FileMonitorService.Models;
using Microsoft.Extensions.Logging;

namespace FileMonitorService.Services;

/// <summary>
/// Мониторинг открытых файлов через PowerShell Get-SmbOpenFile
/// </summary>
public class SmbFileMonitor : ISmbFileMonitor
{
    private readonly ILogger<SmbFileMonitor> _logger;

    public SmbFileMonitor(ILogger<SmbFileMonitor> logger)
    {
        _logger = logger;
    }

    public async Task<List<FileUserInfo>> GetOpenFilesAsync()
    {
        try
        {
            _logger.LogInformation("Получение списка открытых файлов через Get-SmbOpenFile");

            var openFiles = new List<FileUserInfo>();

            await Task.Run(() =>
            {
                using var powerShell = PowerShell.Create();
                
                // Выполнение команды Get-SmbOpenFile
                powerShell.AddCommand("Get-SmbOpenFile");
                
                var results = powerShell.Invoke();

                if (powerShell.HadErrors)
                {
                    foreach (var error in powerShell.Streams.Error)
                    {
                        _logger.LogError("PowerShell ошибка: {Error}", error.ToString());
                    }
                    return;
                }

                foreach (var result in results)
                {
                    try
                    {
                        var fileInfo = new FileUserInfo
                        {
                            FileId = GetPropertyValue<long>(result, "FileId"),
                            SessionId = GetPropertyValue<long>(result, "SessionId"),
                            FilePath = GetPropertyValue<string>(result, "Path") ?? string.Empty,
                            ClientName = GetPropertyValue<string>(result, "ClientComputerName") ?? string.Empty,
                            UserName = GetPropertyValue<string>(result, "ClientUserName") ?? string.Empty,
                            OpenedAt = DateTime.Now, // Get-SmbOpenFile не предоставляет время открытия
                            AccessMode = DetermineAccessMode(result)
                        };

                        if (!string.IsNullOrEmpty(fileInfo.FilePath))
                        {
                            openFiles.Add(fileInfo);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Ошибка при парсинге результата Get-SmbOpenFile");
                    }
                }
            });

            _logger.LogInformation("Найдено {Count} открытых файлов", openFiles.Count);
            return openFiles;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении списка открытых файлов");
            return new List<FileUserInfo>();
        }
    }

    public async Task<List<FileUserInfo>> GetFileUsersAsync(string filePath)
    {
        var allFiles = await GetOpenFilesAsync();
        
        // Нормализация пути для сравнения
        var normalizedPath = NormalizePath(filePath);
        
        return allFiles
            .Where(f => NormalizePath(f.FilePath).Equals(normalizedPath, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public async Task<List<FileUserInfo>> GetUserFilesAsync(string userName)
    {
        var allFiles = await GetOpenFilesAsync();
        
        return allFiles
            .Where(f => f.UserName.Equals(userName, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    private T? GetPropertyValue<T>(PSObject psObject, string propertyName)
    {
        try
        {
            var property = psObject.Properties[propertyName];
            if (property?.Value != null)
            {
                return (T)Convert.ChangeType(property.Value, typeof(T));
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Не удалось получить свойство {Property}", propertyName);
        }

        return default;
    }

    private string DetermineAccessMode(PSObject psObject)
    {
        // Попытка определить режим доступа на основе доступных свойств
        var shareAccess = GetPropertyValue<string>(psObject, "ShareAccess");
        
        if (!string.IsNullOrEmpty(shareAccess))
        {
            return shareAccess;
        }

        // Fallback
        return "Read/Write";
    }

    private string NormalizePath(string path)
    {
        if (string.IsNullOrEmpty(path))
            return string.Empty;

        // Приведение к единому формату
        return path.Replace('/', '\\').TrimEnd('\\').ToLowerInvariant();
    }
}
