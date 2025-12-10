using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace FileMonitorApp.Models
{
    /// <summary>
    /// Результат проверки одного файла с группировкой пользователей
    /// </summary>
    public class FileCheckResult
    {
        public string FilePath { get; set; } = string.Empty;
        public DateTime CheckedAt { get; set; } = DateTime.Now;
        public ObservableCollection<FileUserInfo> Users { get; set; } = new ObservableCollection<FileUserInfo>();
        public bool HasUsers => Users.Count > 0;
        public string StatusIcon => HasUsers ? "🔴" : "✅";
        public string StatusText => HasUsers ? $"Используется ({Users.Count})" : "Свободен";
    }
}
