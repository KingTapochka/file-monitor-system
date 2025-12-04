using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;
using FileMonitorService.Models;
using Microsoft.Extensions.Logging;

namespace FileMonitorService.Services;

/// <summary>
/// Мониторинг открытых файлов через PowerShell Get-SmbOpenFile
/// </summary>
public class SmbFileMonitor : ISmbFileMonitor
{
    private readonly ILogger<SmbFileMonitor> _logger;
    
    // Кэш для резолвинга IP -> Hostname (чтобы не делать DNS запросы каждый раз)
    private readonly ConcurrentDictionary<string, string> _hostnameCache = new();

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

            // PowerShell скрипт для получения открытых файлов в JSON
            var script = @"
                Get-SmbOpenFile | Select-Object FileId, SessionId, Path, ClientComputerName, ClientUserName, ShareRelativePath | 
                ConvertTo-Json -Compress
            ";

            var result = await RunPowerShellAsync(script);

            if (string.IsNullOrWhiteSpace(result))
            {
                _logger.LogInformation("Нет открытых файлов или ошибка выполнения");
                return openFiles;
            }

            try
            {
                // Парсим JSON результат
                using var doc = JsonDocument.Parse(result);
                var root = doc.RootElement;

                // Может быть массив или один объект
                if (root.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in root.EnumerateArray())
                    {
                        var fileInfo = ParseFileInfo(item);
                        if (fileInfo != null)
                            openFiles.Add(fileInfo);
                    }
                }
                else if (root.ValueKind == JsonValueKind.Object)
                {
                    var fileInfo = ParseFileInfo(root);
                    if (fileInfo != null)
                        openFiles.Add(fileInfo);
                }
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Ошибка парсинга JSON: {Result}", result);
            }

            _logger.LogInformation("Найдено {Count} открытых файлов", openFiles.Count);
            return openFiles;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении списка открытых файлов");
            return new List<FileUserInfo>();
        }
    }

    private FileUserInfo? ParseFileInfo(JsonElement element)
    {
        try
        {
            var path = element.TryGetProperty("Path", out var pathProp) ? pathProp.GetString() : null;
            
            if (string.IsNullOrEmpty(path))
                return null;

            var clientIpOrName = element.TryGetProperty("ClientComputerName", out var clientProp) ? clientProp.GetString() ?? "" : "";
            
            // Резолвим IP в hostname
            var clientName = ResolveHostname(clientIpOrName);

            return new FileUserInfo
            {
                FileId = element.TryGetProperty("FileId", out var fileIdProp) ? fileIdProp.GetInt64() : 0,
                SessionId = element.TryGetProperty("SessionId", out var sessionIdProp) ? sessionIdProp.GetInt64() : 0,
                FilePath = path,
                ClientName = clientName,
                UserName = element.TryGetProperty("ClientUserName", out var userProp) ? userProp.GetString() ?? "" : "",
                OpenedAt = DateTime.Now,
                AccessMode = "Read/Write"
            };
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Резолвит IP адрес в имя компьютера (hostname)
    /// </summary>
    private string ResolveHostname(string ipOrHostname)
    {
        if (string.IsNullOrEmpty(ipOrHostname))
            return "";

        // Проверяем кэш
        if (_hostnameCache.TryGetValue(ipOrHostname, out var cachedHostname))
        {
            return cachedHostname;
        }

        string hostname = ipOrHostname;

        try
        {
            // Проверяем, является ли это IP-адресом
            if (IPAddress.TryParse(ipOrHostname, out var ipAddress))
            {
                // Пытаемся получить hostname через DNS
                var hostEntry = Dns.GetHostEntry(ipAddress);
                if (!string.IsNullOrEmpty(hostEntry.HostName))
                {
                    // Берём только имя компьютера без домена
                    hostname = hostEntry.HostName.Split('.')[0].ToUpperInvariant();
                    _logger.LogDebug("Резолвинг IP {IP} -> {Hostname}", ipOrHostname, hostname);
                }
            }
            else
            {
                // Уже hostname, просто нормализуем
                hostname = ipOrHostname.Split('.')[0].ToUpperInvariant();
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Не удалось резолвить {IpOrHostname}, используем как есть", ipOrHostname);
            hostname = ipOrHostname;
        }

        // Сохраняем в кэш
        _hostnameCache.TryAdd(ipOrHostname, hostname);
        
        return hostname;
    }

    private async Task<string> RunPowerShellAsync(string script)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{script.Replace("\"", "\\\"")}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8
            };

            using var process = new Process { StartInfo = psi };
            var output = new StringBuilder();
            var error = new StringBuilder();

            process.OutputDataReceived += (s, e) => { if (e.Data != null) output.AppendLine(e.Data); };
            process.ErrorDataReceived += (s, e) => { if (e.Data != null) error.AppendLine(e.Data); };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync();

            if (error.Length > 0)
            {
                _logger.LogWarning("PowerShell stderr: {Error}", error.ToString());
            }

            return output.ToString().Trim();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка запуска PowerShell");
            return string.Empty;
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

    private string NormalizePath(string path)
    {
        if (string.IsNullOrEmpty(path))
            return string.Empty;

        // Приведение к единому формату
        return path.Replace('/', '\\').TrimEnd('\\').ToLowerInvariant();
    }
}
