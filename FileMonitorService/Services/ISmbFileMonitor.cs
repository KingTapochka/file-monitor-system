using FileMonitorService.Models;

namespace FileMonitorService.Services;

/// <summary>
/// Интерфейс для мониторинга открытых файлов через SMB
/// </summary>
public interface ISmbFileMonitor
{
    /// <summary>
    /// Получить список всех открытых файлов
    /// </summary>
    Task<List<FileUserInfo>> GetOpenFilesAsync();

    /// <summary>
    /// Получить пользователей для конкретного файла
    /// </summary>
    Task<List<FileUserInfo>> GetFileUsersAsync(string filePath);

    /// <summary>
    /// Получить все файлы конкретного пользователя
    /// </summary>
    Task<List<FileUserInfo>> GetUserFilesAsync(string userName);
}
