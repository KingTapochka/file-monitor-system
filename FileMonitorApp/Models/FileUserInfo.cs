using System;
using Newtonsoft.Json;

namespace FileMonitorApp.Models
{
    /// <summary>
    /// Информация о пользователе, открывшем файл
    /// </summary>
    public class FileUserInfo
    {
        /// <summary>
        /// Имя пользователя (домен\имя)
        /// </summary>
        [JsonProperty("userName")]
        public string UserName { get; set; } = string.Empty;

        /// <summary>
        /// Имя компьютера клиента
        /// </summary>
        [JsonProperty("clientComputerName")]
        public string ClientComputerName { get; set; } = string.Empty;

        /// <summary>
        /// Режим доступа (Read, Write, Read/Write)
        /// </summary>
        [JsonProperty("accessMode")]
        public string AccessMode { get; set; } = string.Empty;

        /// <summary>
        /// Время открытия файла
        /// </summary>
        [JsonProperty("openTime")]
        public DateTime OpenTime { get; set; }

        /// <summary>
        /// Путь к файлу на сервере
        /// </summary>
        [JsonProperty("filePath")]
        public string FilePath { get; set; } = string.Empty;

        /// <summary>
        /// ID сессии SMB
        /// </summary>
        [JsonProperty("sessionId")]
        public long SessionId { get; set; }

        /// <summary>
        /// ID файла
        /// </summary>
        [JsonProperty("fileId")]
        public long FileId { get; set; }
    }

    /// <summary>
    /// Ответ API со списком пользователей файла
    /// </summary>
    public class FileUsersResponse
    {
        /// <summary>
        /// Путь к файлу
        /// </summary>
        [JsonProperty("filePath")]
        public string FilePath { get; set; } = string.Empty;

        /// <summary>
        /// Список пользователей
        /// </summary>
        [JsonProperty("users")]
        public FileUserInfo[] Users { get; set; } = Array.Empty<FileUserInfo>();

        /// <summary>
        /// Количество пользователей
        /// </summary>
        [JsonProperty("userCount")]
        public int UserCount { get; set; }

        /// <summary>
        /// Время запроса
        /// </summary>
        [JsonProperty("queryTime")]
        public DateTime QueryTime { get; set; }
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
        /// Количество открытий
        /// </summary>
        public int OpenCount { get; set; }

        /// <summary>
        /// Список пользователей
        /// </summary>
        public string[] Users { get; set; } = Array.Empty<string>();
    }

    /// <summary>
    /// Ответ API со списком активных файлов
    /// </summary>
    public class ActiveFilesResponse
    {
        /// <summary>
        /// Список активных файлов
        /// </summary>
        public ActiveFileInfo[] Files { get; set; } = Array.Empty<ActiveFileInfo>();

        /// <summary>
        /// Общее количество файлов
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// Время запроса
        /// </summary>
        public DateTime QueryTime { get; set; }
    }

    /// <summary>
    /// Ответ API о состоянии здоровья сервиса
    /// </summary>
    public class HealthResponse
    {
        /// <summary>
        /// Статус сервиса
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Версия сервиса
        /// </summary>
        public string Version { get; set; } = string.Empty;

        /// <summary>
        /// Время работы
        /// </summary>
        public string Uptime { get; set; } = string.Empty;

        /// <summary>
        /// Время последнего сканирования
        /// </summary>
        public DateTime? LastScan { get; set; }
    }
}
