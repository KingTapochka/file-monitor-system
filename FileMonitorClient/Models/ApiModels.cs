using System;
using System.Collections.Generic;

namespace FileMonitorClient.Models
{
    /// <summary>
    /// Информация о пользователе, который открыл файл
    /// </summary>
    public class FileUserInfo
    {
        public string UserName { get; set; } = string.Empty;
        public string ClientName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string AccessMode { get; set; } = string.Empty;
        public DateTime OpenedAt { get; set; }
        public long SessionId { get; set; }
        public long FileId { get; set; }
    }

    /// <summary>
    /// Ответ с информацией о файле и его пользователях
    /// </summary>
    public class FileUsersResponse
    {
        public string FilePath { get; set; } = string.Empty;
        public List<FileUserInfo> Users { get; set; } = new List<FileUserInfo>();
        public DateTime LastUpdated { get; set; }
        public int UserCount { get; set; }
    }

    /// <summary>
    /// Ответ об ошибке API
    /// </summary>
    public class ApiErrorResponse
    {
        public string Message { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
    }
}
