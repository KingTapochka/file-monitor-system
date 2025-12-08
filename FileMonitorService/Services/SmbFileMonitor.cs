using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using FileMonitorService.Models;
using Microsoft.Extensions.Logging;

namespace FileMonitorService.Services;

/// <summary>
/// Мониторинг открытых файлов через PowerShell Get-SmbOpenFile и Sysinternals Handle
/// </summary>
public class SmbFileMonitor : ISmbFileMonitor
{
    private readonly ILogger<SmbFileMonitor> _logger;
    
    // Кэш для резолвинга IP -> Hostname (чтобы не делать DNS запросы каждый раз)
    private readonly ConcurrentDictionary<string, string> _hostnameCache = new();
    
    // Путь к Handle.exe (Sysinternals)
    private readonly string _handlePath;
    private bool _handleAvailable = false;

    public SmbFileMonitor(ILogger<SmbFileMonitor> logger)
    {
        _logger = logger;
        
        // Ищем Handle.exe в нескольких местах
        var possiblePaths = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "handle64.exe"),
            Path.Combine(AppContext.BaseDirectory, "handle.exe"),
            @"C:\Sysinternals\handle64.exe",
            @"C:\Sysinternals\handle.exe",
            @"C:\Tools\handle64.exe",
            @"C:\Program Files\Sysinternals\handle64.exe"
        };
        
        _handlePath = possiblePaths.FirstOrDefault(File.Exists) ?? "";
        _handleAvailable = !string.IsNullOrEmpty(_handlePath);
        
        if (_handleAvailable)
        {
            _logger.LogInformation("Handle.exe найден: {Path}", _handlePath);
        }
        else
        {
            _logger.LogWarning("Handle.exe не найден. Для полного мониторинга скачайте с https://docs.microsoft.com/sysinternals/downloads/handle");
        }
    }

    public async Task<List<FileUserInfo>> GetOpenFilesAsync()
    {
        try
        {
            _logger.LogInformation("Получение списка открытых файлов");

            var openFiles = new List<FileUserInfo>();

            // 1. Получаем файлы через SMB (сетевые подключения)
            var smbFiles = await GetSmbOpenFilesAsync();
            openFiles.AddRange(smbFiles);

            // 2. Получаем файлы через openfiles.exe (локальные открытия)
            var localFiles = await GetLocalOpenFilesAsync();
            AddUniqueFiles(openFiles, localFiles);
            
            // 3. Получаем файлы через Handle.exe (Sysinternals) - самый надёжный метод
            if (_handleAvailable)
            {
                var handleFiles = await GetHandleOpenFilesAsync();
                AddUniqueFiles(openFiles, handleFiles);
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

    /// <summary>
    /// Получение открытых файлов через SMB (сетевые подключения)
    /// Использует несколько методов для максимального охвата
    /// </summary>
    private async Task<List<FileUserInfo>> GetSmbOpenFilesAsync()
    {
        var openFiles = new List<FileUserInfo>();

        try
        {
            // Метод 1: Get-SmbOpenFile (основной)
            var script = @"
                $files = @()
                
                # Получаем все открытые файлы через SMB
                try {
                    Get-SmbOpenFile -ErrorAction SilentlyContinue | ForEach-Object {
                        $files += @{
                            FileId = $_.FileId
                            SessionId = $_.SessionId
                            Path = $_.Path
                            ClientComputerName = $_.ClientComputerName
                            ClientUserName = $_.ClientUserName
                            ShareRelativePath = $_.ShareRelativePath
                        }
                    }
                } catch {}
                
                # Метод 2: net file (дополнительный, может показать файлы которые не видит Get-SmbOpenFile)
                try {
                    $netOutput = net file 2>$null
                    if ($netOutput) {
                        $currentFile = $null
                        foreach ($line in $netOutput) {
                            if ($line -match '^\s*(\d+)\s+(.+?)\s+(\S+)\s+(\d+)') {
                                $id = $matches[1]
                                $path = $matches[2].Trim()
                                $user = $matches[3]
                                
                                # Проверяем, нет ли уже этого файла
                                $exists = $files | Where-Object { $_.Path -eq $path -and $_.ClientUserName -like ""*$user*"" }
                                if (-not $exists -and $path) {
                                    $files += @{
                                        FileId = [long]$id
                                        SessionId = 0
                                        Path = $path
                                        ClientComputerName = ''
                                        ClientUserName = $user
                                        ShareRelativePath = $path
                                    }
                                }
                            }
                        }
                    }
                } catch {}
                
                $files | ConvertTo-Json -Compress -Depth 3
            ";

            var result = await RunPowerShellAsync(script);

            if (string.IsNullOrWhiteSpace(result) || result == "null" || result == "[]")
            {
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
                        var fileInfo = ParseSmbFileInfo(item);
                        if (fileInfo != null)
                            openFiles.Add(fileInfo);
                    }
                }
                else if (root.ValueKind == JsonValueKind.Object)
                {
                    var fileInfo = ParseSmbFileInfo(root);
                    if (fileInfo != null)
                        openFiles.Add(fileInfo);
                }
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Ошибка парсинга JSON SMB: {Result}", result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Ошибка получения SMB файлов");
        }

        return openFiles;
    }

    /// <summary>
    /// Получение открытых файлов через openfiles.exe (включая локальные)
    /// Требует включенной опции: openfiles /local on (и перезагрузки)
    /// </summary>
    private async Task<List<FileUserInfo>> GetLocalOpenFilesAsync()
    {
        var openFiles = new List<FileUserInfo>();

        try
        {
            // Используем openfiles.exe для получения локально открытых файлов
            var script = @"
                $result = @()
                try {
                    $output = openfiles /query /fo csv /nh 2>$null
                    if ($output) {
                        $output | ForEach-Object {
                            if ($_ -match '""([^""]+)"",""([^""]+)"",""([^""]+)""') {
                                $result += @{
                                    ID = $matches[1]
                                    User = $matches[2]
                                    Path = $matches[3]
                                }
                            }
                        }
                    }
                } catch {}
                $result | ConvertTo-Json -Compress
            ";

            var result = await RunPowerShellAsync(script);

            if (string.IsNullOrWhiteSpace(result) || result == "null")
            {
                return openFiles;
            }

            try
            {
                using var doc = JsonDocument.Parse(result);
                var root = doc.RootElement;

                var items = root.ValueKind == JsonValueKind.Array 
                    ? root.EnumerateArray().ToList() 
                    : new List<JsonElement> { root };

                foreach (var item in items)
                {
                    var path = item.TryGetProperty("Path", out var pathProp) ? pathProp.GetString() : null;
                    var user = item.TryGetProperty("User", out var userProp) ? userProp.GetString() : null;

                    if (!string.IsNullOrEmpty(path) && !string.IsNullOrEmpty(user))
                    {
                        openFiles.Add(new FileUserInfo
                        {
                            FilePath = path,
                            UserName = user,
                            ClientName = Environment.MachineName,
                            OpenedAt = DateTime.Now,
                            AccessMode = "Local"
                        });
                    }
                }
            }
            catch (JsonException ex)
            {
                _logger.LogDebug(ex, "Ошибка парсинга openfiles результата");
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "openfiles.exe недоступен или требует включения");
        }

        return openFiles;
    }

    private FileUserInfo? ParseSmbFileInfo(JsonElement element)
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

    /// <summary>
    /// Добавляет уникальные файлы в список (без дубликатов)
    /// </summary>
    private void AddUniqueFiles(List<FileUserInfo> target, List<FileUserInfo> source)
    {
        foreach (var file in source)
        {
            var normalizedPath = NormalizePath(file.FilePath);
            if (!target.Any(f => NormalizePath(f.FilePath).Equals(normalizedPath, StringComparison.OrdinalIgnoreCase) 
                && f.UserName.Equals(file.UserName, StringComparison.OrdinalIgnoreCase)))
            {
                target.Add(file);
            }
        }
    }

    /// <summary>
    /// Получение открытых файлов через Sysinternals Handle.exe
    /// Это самый надёжный метод, видит все открытые файлы включая те что открыты Adobe Acrobat
    /// </summary>
    private async Task<List<FileUserInfo>> GetHandleOpenFilesAsync()
    {
        var openFiles = new List<FileUserInfo>();

        if (!_handleAvailable)
            return openFiles;

        try
        {
            // Запускаем handle.exe для поиска открытых файлов
            // Фильтруем только файлы (не реестр и прочее)
            var psi = new ProcessStartInfo
            {
                FileName = _handlePath,
                Arguments = "-accepteula -u -nobanner",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8
            };

            using var process = new Process { StartInfo = psi };
            var output = new StringBuilder();

            process.OutputDataReceived += (s, e) => { if (e.Data != null) output.AppendLine(e.Data); };

            process.Start();
            process.BeginOutputReadLine();

            // Таймаут 30 секунд
            var completed = await Task.Run(() => process.WaitForExit(30000));
            if (!completed)
            {
                process.Kill();
                _logger.LogWarning("Handle.exe превысил таймаут");
                return openFiles;
            }

            // Парсим вывод handle.exe
            // Формат: ProcessName pid: Type (RW-) UserName: Path
            // Пример: Acrobat.exe pid: 1234: File  (RW-)   DOMAIN\user: D:\Files\document.pdf
            var lines = output.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries);
            
            string currentProcess = "";
            string currentUser = "";
            
            // Регулярка для парсинга строки заголовка процесса
            var processHeaderRegex = new Regex(@"^(.+?)\s+pid:\s*(\d+)\s+(.+)$", RegexOptions.IgnoreCase);
            // Регулярка для парсинга строки файла
            var fileLineRegex = new Regex(@"^\s*([A-F0-9]+):\s+File\s+\([^)]+\)\s+(.+)$", RegexOptions.IgnoreCase);

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                if (string.IsNullOrEmpty(trimmedLine)) continue;

                // Проверяем заголовок процесса
                var headerMatch = processHeaderRegex.Match(trimmedLine);
                if (headerMatch.Success)
                {
                    currentProcess = headerMatch.Groups[1].Value.Trim();
                    // Извлекаем пользователя из строки если есть
                    var userPart = headerMatch.Groups[3].Value;
                    if (userPart.Contains('\\'))
                    {
                        currentUser = userPart.Trim();
                    }
                    continue;
                }

                // Проверяем строку файла
                var fileMatch = fileLineRegex.Match(trimmedLine);
                if (fileMatch.Success)
                {
                    var filePath = fileMatch.Groups[2].Value.Trim();
                    
                    // Фильтруем только реальные файлы на диске (не системные объекты)
                    if (IsRealFilePath(filePath))
                    {
                        openFiles.Add(new FileUserInfo
                        {
                            FilePath = filePath,
                            UserName = currentUser,
                            ClientName = Environment.MachineName,
                            OpenedAt = DateTime.Now,
                            AccessMode = $"Handle ({currentProcess})"
                        });
                    }
                }
            }

            _logger.LogDebug("Handle.exe нашёл {Count} открытых файлов", openFiles.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Ошибка при запуске Handle.exe");
        }

        return openFiles;
    }

    /// <summary>
    /// Проверяет, является ли путь реальным файлом на диске
    /// </summary>
    private bool IsRealFilePath(string path)
    {
        if (string.IsNullOrEmpty(path)) return false;
        
        // Должен начинаться с буквы диска или UNC пути
        if (path.Length < 3) return false;
        
        // Локальный путь (C:\...)
        if (char.IsLetter(path[0]) && path[1] == ':' && path[2] == '\\')
            return true;
            
        // UNC путь (\\server\share\...)
        if (path.StartsWith(@"\\") && path.Length > 4)
            return true;
            
        return false;
    }

    private string NormalizePath(string path)
    {
        if (string.IsNullOrEmpty(path))
            return string.Empty;

        // Приведение к единому формату
        return path.Replace('/', '\\').TrimEnd('\\').ToLowerInvariant();
    }
}
