using FileMonitorService.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FileMonitorService;

/// <summary>
/// Фоновый сервис для периодического обновления кэша открытых файлов
/// </summary>
public class FileMonitorWorker : BackgroundService
{
    private readonly ILogger<FileMonitorWorker> _logger;
    private readonly ISmbFileMonitor _smbMonitor;
    private readonly IFileMonitorCache _cache;
    private readonly TimeSpan _refreshInterval;

    public FileMonitorWorker(
        ILogger<FileMonitorWorker> logger,
        ISmbFileMonitor smbMonitor,
        IFileMonitorCache cache,
        IConfiguration configuration)
    {
        _logger = logger;
        _smbMonitor = smbMonitor;
        _cache = cache;
        
        // Чтение интервала из конфигурации
        var intervalSeconds = configuration.GetValue<int>("FileMonitor:RefreshIntervalSeconds", 10);
        _refreshInterval = TimeSpan.FromSeconds(intervalSeconds);
        
        _logger.LogInformation("Интервал обновления: {Interval} секунд", intervalSeconds);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("FileMonitorWorker запущен");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogDebug("Обновление списка открытых файлов...");

                var openFiles = await _smbMonitor.GetOpenFilesAsync();
                _cache.UpdateCache(openFiles);

                _logger.LogInformation(
                    "Обновлено: {Count} открытых файлов. Следующее обновление через {Interval} сек",
                    openFiles.Count,
                    _refreshInterval.TotalSeconds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обновлении кэша файлов");
            }

            await Task.Delay(_refreshInterval, stoppingToken);
        }

        _logger.LogInformation("FileMonitorWorker остановлен");
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("FileMonitorWorker стартует...");
        return base.StartAsync(cancellationToken);
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("FileMonitorWorker останавливается...");
        return base.StopAsync(cancellationToken);
    }
}
