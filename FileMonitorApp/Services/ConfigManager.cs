using System;
using System.IO;
using Newtonsoft.Json;

namespace FileMonitorApp.Services
{
    /// <summary>
    /// Менеджер настроек приложения (сохраняет в папку пользователя)
    /// </summary>
    public static class ConfigManager
    {
        private static AppSettings? _settings;
        private static readonly string SettingsFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FileMonitorApp");
        private static readonly string SettingsFile = Path.Combine(SettingsFolder, "settings.json");

        private static AppSettings Settings
        {
            get
            {
                if (_settings == null)
                {
                    LoadSettings();
                }
                return _settings!;
            }
        }

        public static string ServerAddress
        {
            get => Settings.ServerAddress;
            set
            {
                Settings.ServerAddress = value;
                SaveSettings();
            }
        }

        public static string HotKey
        {
            get => Settings.HotKey;
            set
            {
                Settings.HotKey = value;
                SaveSettings();
            }
        }

        public static bool AutoStart
        {
            get => Settings.AutoStart;
            set
            {
                Settings.AutoStart = value;
                SaveSettings();
                UpdateAutoStart(value);
            }
        }

        private static void LoadSettings()
        {
            try
            {
                if (File.Exists(SettingsFile))
                {
                    var json = File.ReadAllText(SettingsFile);
                    _settings = JsonConvert.DeserializeObject<AppSettings>(json) ?? new AppSettings();
                }
                else
                {
                    _settings = new AppSettings();
                }
            }
            catch
            {
                _settings = new AppSettings();
            }
        }

        private static void SaveSettings()
        {
            try
            {
                // Создаем папку если не существует
                if (!Directory.Exists(SettingsFolder))
                {
                    Directory.CreateDirectory(SettingsFolder);
                }

                var json = JsonConvert.SerializeObject(_settings, Formatting.Indented);
                File.WriteAllText(SettingsFile, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка сохранения настроек: {ex.Message}");
            }
        }

        private static void UpdateAutoStart(bool enable)
        {
            try
            {
                var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
                
                if (key == null) return;

                if (enable)
                {
                    var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
                    if (!string.IsNullOrEmpty(exePath))
                    {
                        key.SetValue("FileMonitorApp", $"\"{exePath}\"");
                    }
                }
                else
                {
                    key.DeleteValue("FileMonitorApp", false);
                }
            }
            catch
            {
                // Игнорируем ошибки реестра
            }
        }

        /// <summary>
        /// Класс для хранения настроек
        /// </summary>
        private class AppSettings
        {
            public string ServerAddress { get; set; } = "http://localhost:5000";
            public string HotKey { get; set; } = "Win+Shift+F";
            public bool AutoStart { get; set; } = false;
        }
    }
}
