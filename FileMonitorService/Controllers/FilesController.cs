using FileMonitorService.Models;
using FileMonitorService.Services;
using Microsoft.AspNetCore.Mvc;

namespace FileMonitorService.Controllers;

/// <summary>
/// API контроллер для получения информации об открытых файлах
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class FilesController : ControllerBase
{
    private readonly ILogger<FilesController> _logger;
    private readonly IFileMonitorCache _cache;
    private readonly ISmbFileMonitor _smbMonitor;

    public FilesController(
        ILogger<FilesController> logger,
        IFileMonitorCache cache,
        ISmbFileMonitor smbMonitor)
    {
        _logger = logger;
        _cache = cache;
        _smbMonitor = smbMonitor;
    }

    /// <summary>
    /// Получить список пользователей, которые открыли указанный файл
    /// </summary>
    /// <param name="filePath">Полный путь к файлу</param>
    /// <returns>Список пользователей</returns>
    [HttpGet("users")]
    [ProducesResponseType(typeof(FileUsersResponse), 200)]
    [ProducesResponseType(404)]
    public ActionResult<FileUsersResponse> GetFileUsers([FromQuery] string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return BadRequest("Путь к файлу не может быть пустым");
        }

        _logger.LogInformation("Запрос пользователей для файла: {FilePath}", filePath);

        var result = _cache.GetFileUsers(filePath);

        if (result == null || result.Users.Count == 0)
        {
            _logger.LogInformation("Файл не открыт: {FilePath}", filePath);
            return NotFound(new { message = "Файл не открыт или не найден", filePath });
        }

        return Ok(result);
    }

    /// <summary>
    /// Получить список всех активных (открытых) файлов
    /// </summary>
    /// <returns>Список активных файлов</returns>
    [HttpGet("active")]
    [ProducesResponseType(typeof(List<ActiveFileInfo>), 200)]
    public ActionResult<List<ActiveFileInfo>> GetActiveFiles()
    {
        _logger.LogInformation("Запрос списка активных файлов");

        var activeFiles = _cache.GetActiveFiles();

        return Ok(new
        {
            count = activeFiles.Count,
            files = activeFiles
        });
    }

    /// <summary>
    /// Получить список файлов, открытых конкретным пользователем
    /// </summary>
    /// <param name="userName">Имя пользователя</param>
    /// <returns>Список файлов пользователя</returns>
    [HttpGet("user/{userName}")]
    [ProducesResponseType(typeof(List<FileUserInfo>), 200)]
    public ActionResult<List<FileUserInfo>> GetUserFiles(string userName)
    {
        if (string.IsNullOrWhiteSpace(userName))
        {
            return BadRequest("Имя пользователя не может быть пустым");
        }

        _logger.LogInformation("Запрос файлов пользователя: {UserName}", userName);

        var files = _cache.GetUserFiles(userName);

        return Ok(new
        {
            userName,
            count = files.Count,
            files
        });
    }

    /// <summary>
    /// Принудительно обновить кэш (для тестирования)
    /// </summary>
    [HttpPost("refresh")]
    [ProducesResponseType(200)]
    public async Task<ActionResult> RefreshCache()
    {
        _logger.LogInformation("Принудительное обновление кэша");

        try
        {
            var openFiles = await _smbMonitor.GetOpenFilesAsync();
            _cache.UpdateCache(openFiles);

            return Ok(new
            {
                message = "Кэш обновлен",
                filesCount = openFiles.Count,
                timestamp = DateTime.Now
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при обновлении кэша");
            return StatusCode(500, new { message = "Ошибка при обновлении кэша", error = ex.Message });
        }
    }

    /// <summary>
    /// Проверка работоспособности сервиса
    /// </summary>
    [HttpGet("health")]
    [ProducesResponseType(200)]
    public ActionResult HealthCheck()
    {
        return Ok(new
        {
            status = "healthy",
            service = "FileMonitorService",
            timestamp = DateTime.Now
        });
    }
}
