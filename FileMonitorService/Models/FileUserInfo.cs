namespace FileMonitorService.Models;

/// <summary>
/// Информация о пользователе, который открыл файл
/// </summary>
public class FileUserInfo
{
    /// <summary>
    /// Имя пользователя
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// Имя компьютера
    /// </summary>
    public string ClientName { get; set; } = string.Empty;

    /// <summary>
    /// Полный путь к файлу
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Права доступа (Read/Write)
    /// </summary>
    public string AccessMode { get; set; } = string.Empty;

    /// <summary>
    /// Время открытия файла
    /// </summary>
    public DateTime OpenedAt { get; set; }

    /// <summary>
    /// ID сессии SMB
    /// </summary>
    public long SessionId { get; set; }

    /// <summary>
    /// ID открытого файла
    /// </summary>
    public long FileId { get; set; }
}

/// <summary>
/// Ответ с информацией о файле и его пользователях
/// </summary>
public class FileUsersResponse
{
    /// <summary>
    /// Путь к файлу
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Список пользователей
    /// </summary>
    public List<FileUserInfo> Users { get; set; } = new();

    /// <summary>
    /// Время последнего обновления
    /// </summary>
    public DateTime LastUpdated { get; set; }

    /// <summary>
    /// Количество пользователей
    /// </summary>
    public int UserCount => Users.Count;
}

/// <summary>
/// Информация об активном файле
/// </summary>
public class ActiveFileInfo
{
    /// <summary>
    /// Путь к файлу
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Количество пользователей
    /// </summary>
    public int UserCount { get; set; }

    /// <summary>
    /// Время последнего доступа
    /// </summary>
    public DateTime LastAccess { get; set; }
}
