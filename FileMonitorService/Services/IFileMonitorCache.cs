using FileMonitorService.Models;

namespace FileMonitorService.Services;

/// <summary>
/// Интерфейс для кэширования данных мониторинга файлов
/// </summary>
public interface IFileMonitorCache
{
    /// <summary>
    /// Обновить кэш открытых файлов
    /// </summary>
    void UpdateCache(List<FileUserInfo> openFiles);

    /// <summary>
    /// Получить пользователей файла из кэша
    /// </summary>
    FileUsersResponse? GetFileUsers(string filePath);

    /// <summary>
    /// Получить все активные файлы
    /// </summary>
    List<ActiveFileInfo> GetActiveFiles();

    /// <summary>
    /// Получить файлы пользователя
    /// </summary>
    List<FileUserInfo> GetUserFiles(string userName);

    /// <summary>
    /// Очистить кэш
    /// </summary>
    void ClearCache();
}
