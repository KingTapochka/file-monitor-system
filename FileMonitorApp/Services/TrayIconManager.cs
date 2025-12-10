using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Input;
using Hardcodet.Wpf.TaskbarNotification;
using NHotkey;
using NHotkey.Wpf;
using FileMonitorApp.Views;

namespace FileMonitorApp.Services
{
    /// <summary>
    /// Менеджер иконки в системном трее
    /// </summary>
    public class TrayIconManager : IDisposable
    {
        private TaskbarIcon? _trayIcon;
        private FileCheckWindow? _checkWindow;
        private FileUsersWindow? _usersWindow; // Переиспользуемое окно результатов

        public void Initialize()
        {
            // Создаем иконку в трее
            _trayIcon = new TaskbarIcon
            {
                Icon = CreateDefaultIcon(),
                ToolTipText = "File Monitor - Проверка использования файлов\nWin+Shift+F - проверить файл",
                Visibility = Visibility.Visible
            };

            // Контекстное меню трея
            var contextMenu = new System.Windows.Controls.ContextMenu();
            
            var checkFileItem = new System.Windows.Controls.MenuItem { Header = "🔍 Проверить файл..." };
            checkFileItem.Click += (s, e) => ShowCheckWindow();
            
            var settingsItem = new System.Windows.Controls.MenuItem { Header = "⚙️ Настройки..." };
            settingsItem.Click += (s, e) => ShowSettings();
            
            var separatorItem = new System.Windows.Controls.Separator();
            
            var exitItem = new System.Windows.Controls.MenuItem { Header = "❌ Выход" };
            exitItem.Click += (s, e) => Application.Current.Shutdown();

            contextMenu.Items.Add(checkFileItem);
            contextMenu.Items.Add(settingsItem);
            contextMenu.Items.Add(separatorItem);
            contextMenu.Items.Add(exitItem);

            _trayIcon.ContextMenu = contextMenu;

            // Двойной клик - открыть окно проверки
            _trayIcon.TrayMouseDoubleClick += (s, e) => ShowCheckWindow();

            // Регистрация глобальной горячей клавиши Win+Shift+F
            try
            {
                HotkeyManager.Current.AddOrReplace("CheckFile", 
                    Key.F, 
                    ModifierKeys.Windows | ModifierKeys.Shift, 
                    OnHotKeyPressed);
                
                System.Diagnostics.Debug.WriteLine("Горячая клавиша Win+Shift+F зарегистрирована");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Не удалось зарегистрировать горячую клавишу: {ex.Message}");
                ShowBalloon("File Monitor", 
                    "Не удалось зарегистрировать горячую клавишу Win+Shift+F", 
                    BalloonIcon.Warning);
            }
        }

        private void OnHotKeyPressed(object? sender, HotkeyEventArgs e)
        {
            ShowCheckWindow();
            e.Handled = true;
        }

        public void CheckFile(string filePath)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                // Переиспользуем одно окно вместо создания нового
                if (_usersWindow == null || !_usersWindow.IsLoaded)
                {
                    _usersWindow = new FileUsersWindow(filePath);
                    _usersWindow.Closed += (s, e) => _usersWindow = null;
                    _usersWindow.Show();
                }
                else
                {
                    // Обновляем информацию о файле в существующем окне
                    _usersWindow.UpdateFileInfo(filePath);
                    _usersWindow.Show();
                }
                
                _usersWindow.Activate();
            });
        }

        /// <summary>
        /// Проверка файла или папки (папки игнорируются)
        /// </summary>
        public void CheckPath(string path)
        {
            if (File.Exists(path))
            {
                CheckFile(path);
            }
        }

        private void ShowCheckWindow()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (_checkWindow == null || !_checkWindow.IsVisible)
                {
                    _checkWindow = new FileCheckWindow();
                    _checkWindow.FileSelected += (s, path) => 
                    {
                        if (!string.IsNullOrEmpty(path))
                        {
                            CheckFile(path);
                        }
                    };
                }
                
                _checkWindow.Show();
                _checkWindow.Activate();
            });
        }

        private void ShowSettings()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var window = new SettingsWindow();
                window.ShowDialog();
            });
        }

        public void ShowBalloon(string title, string message, BalloonIcon icon = BalloonIcon.Info)
        {
            _trayIcon?.ShowBalloonTip(title, message, icon);
        }

        private Icon CreateDefaultIcon()
        {
            // Пробуем загрузить иконку из ресурсов
            try
            {
                var iconPath = System.IO.Path.Combine(AppContext.BaseDirectory, "app.ico");
                
                if (System.IO.File.Exists(iconPath))
                {
                    return new Icon(iconPath);
                }
            }
            catch { }

            // Создаем простую иконку программно (fallback)
            using var bitmap = new Bitmap(32, 32);
            using var graphics = Graphics.FromImage(bitmap);
            
            graphics.Clear(Color.Transparent);
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            
            // Синий круг
            using var blueBrush = new SolidBrush(Color.FromArgb(33, 150, 243));
            graphics.FillEllipse(blueBrush, 2, 2, 28, 28);
            
            // Белая буква F
            using var font = new Font("Segoe UI", 14, System.Drawing.FontStyle.Bold);
            using var whiteBrush = new SolidBrush(Color.White);
            graphics.DrawString("F", font, whiteBrush, 8, 5);
            
            return Icon.FromHandle(bitmap.GetHicon());
        }

        public void Dispose()
        {
            try
            {
                HotkeyManager.Current.Remove("CheckFile");
            }
            catch { }
            
            _trayIcon?.Dispose();
        }
    }
}
