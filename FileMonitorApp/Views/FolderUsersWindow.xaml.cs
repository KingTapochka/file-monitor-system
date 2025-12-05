using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using FileMonitorApp.Models;
using FileMonitorApp.Services;

namespace FileMonitorApp.Views
{
    /// <summary>
    /// Информация о занятом файле в папке
    /// </summary>
    public class FolderFileInfo
    {
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string ClientName { get; set; } = string.Empty;
    }

    public partial class FolderUsersWindow : Window
    {
        private readonly string _folderPath;
        private readonly ApiClient _apiClient;

        public FolderUsersWindow(string folderPath)
        {
            InitializeComponent();
            _folderPath = folderPath;
            _apiClient = new ApiClient();
            
            FolderPathText.Text = folderPath;
            FolderPathText.ToolTip = folderPath;
            
            Loaded += FolderUsersWindow_Loaded;
        }

        private async void FolderUsersWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await ScanFolderAsync();
        }

        private async Task ScanFolderAsync()
        {
            ShowLoading();
            
            try
            {
                bool includeSubfolders = IncludeSubfoldersCheckBox.IsChecked == true;
                var busyFiles = new List<FolderFileInfo>();

                // Получаем список файлов в папке
                var searchOption = includeSubfolders 
                    ? SearchOption.AllDirectories 
                    : SearchOption.TopDirectoryOnly;

                string[] files;
                try
                {
                    files = Directory.GetFiles(_folderPath, "*.*", searchOption);
                }
                catch (UnauthorizedAccessException)
                {
                    ShowError("Нет доступа к папке или некоторым подпапкам");
                    return;
                }
                catch (DirectoryNotFoundException)
                {
                    ShowError("Папка не найдена");
                    return;
                }

                LoadingText.Text = $"Проверка файлов: 0 / {files.Length}";
                int checkedCount = 0;
                int batchSize = 10; // Проверяем по 10 файлов параллельно

                // Проверяем файлы пакетами
                for (int i = 0; i < files.Length; i += batchSize)
                {
                    var batch = files.Skip(i).Take(batchSize).ToArray();
                    var tasks = batch.Select(async file =>
                    {
                        try
                        {
                            var users = await _apiClient.GetFileUsersAsync(file);
                            if (users != null && users.Count > 0)
                            {
                                return users.Select(u => new FolderFileInfo
                                {
                                    FileName = Path.GetFileName(file),
                                    FilePath = file,
                                    UserName = u.UserName,
                                    ClientName = u.ClientName
                                }).ToList();
                            }
                        }
                        catch
                        {
                            // Игнорируем ошибки для отдельных файлов
                        }
                        return new List<FolderFileInfo>();
                    });

                    var results = await Task.WhenAll(tasks);
                    foreach (var result in results)
                    {
                        busyFiles.AddRange(result);
                    }

                    checkedCount += batch.Length;
                    LoadingText.Text = $"Проверка файлов: {checkedCount} / {files.Length}";
                }

                if (busyFiles.Count == 0)
                {
                    ShowNoUsers(files.Length);
                }
                else
                {
                    ShowBusyFiles(busyFiles, files.Length);
                }
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка: {ex.Message}");
            }
        }

        private void ShowLoading()
        {
            LoadingPanel.Visibility = Visibility.Visible;
            FilesDataGrid.Visibility = Visibility.Collapsed;
            NoUsersPanel.Visibility = Visibility.Collapsed;
            ErrorPanel.Visibility = Visibility.Collapsed;
            StatusBorder.Visibility = Visibility.Collapsed;
            
            LoadingText.Text = "Сканирование папки...";
            RefreshButton.IsEnabled = false;
        }

        private void ShowBusyFiles(List<FolderFileInfo> files, int totalFiles)
        {
            LoadingPanel.Visibility = Visibility.Collapsed;
            FilesDataGrid.Visibility = Visibility.Visible;
            NoUsersPanel.Visibility = Visibility.Collapsed;
            ErrorPanel.Visibility = Visibility.Collapsed;
            
            FilesDataGrid.ItemsSource = files;

            // Показываем статус
            StatusBorder.Visibility = Visibility.Visible;
            StatusBorder.Background = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(255, 243, 224)); // Оранжевый фон
            StatusBorder.BorderBrush = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(255, 152, 0));
            
            var uniqueFiles = files.Select(f => f.FilePath).Distinct().Count();
            StatusText.Text = $"⚠️ Занято файлов: {uniqueFiles} из {totalFiles} (пользователей: {files.Count})";
            StatusText.Foreground = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(230, 81, 0));

            TitleText.Text = $"📁 Проверка папки — Занято: {uniqueFiles}";
            
            RefreshButton.IsEnabled = true;
        }

        private void ShowNoUsers(int totalFiles)
        {
            LoadingPanel.Visibility = Visibility.Collapsed;
            FilesDataGrid.Visibility = Visibility.Collapsed;
            NoUsersPanel.Visibility = Visibility.Visible;
            ErrorPanel.Visibility = Visibility.Collapsed;
            
            // Показываем статус
            StatusBorder.Visibility = Visibility.Visible;
            StatusBorder.Background = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(232, 245, 233)); // Зелёный фон
            StatusBorder.BorderBrush = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(76, 175, 80));
            StatusText.Text = $"✅ Проверено файлов: {totalFiles} — все свободны";
            StatusText.Foreground = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(46, 125, 50));

            TitleText.Text = "📁 Проверка папки — Всё свободно";
            
            RefreshButton.IsEnabled = true;
        }

        private void ShowError(string message)
        {
            LoadingPanel.Visibility = Visibility.Collapsed;
            FilesDataGrid.Visibility = Visibility.Collapsed;
            NoUsersPanel.Visibility = Visibility.Collapsed;
            ErrorPanel.Visibility = Visibility.Visible;
            StatusBorder.Visibility = Visibility.Collapsed;
            
            ErrorMessage.Text = message;
            
            RefreshButton.IsEnabled = true;
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await ScanFolderAsync();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
