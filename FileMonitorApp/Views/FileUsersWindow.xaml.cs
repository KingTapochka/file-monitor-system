using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using FileMonitorApp.Models;
using FileMonitorApp.Services;

namespace FileMonitorApp.Views
{
    public partial class FileUsersWindow : Window
    {
        private readonly ApiClient _apiClient;
        private readonly ObservableCollection<FileCheckResult> _checkHistory;

        public FileUsersWindow(string filePath)
        {
            InitializeComponent();
            _apiClient = new ApiClient();
            _checkHistory = new ObservableCollection<FileCheckResult>();
            FileTreeView.ItemsSource = _checkHistory;
            
            Loaded += FileUsersWindow_Loaded;
            
            // Добавляем первый файл в историю
            _ = AddFileToHistoryAsync(filePath);
        }

        private async void FileUsersWindow_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateUI();
        }

        // Метод для добавления файла в историю (переиспользование окна)
        public async void UpdateFileInfo(string filePath)
        {
            await AddFileToHistoryAsync(filePath);
        }

        private async Task AddFileToHistoryAsync(string filePath)
        {
            ShowLoading();
            
            try
            {
                var users = await _apiClient.GetFileUsersAsync(filePath);
                
                if (users == null)
                {
                    ShowError("Не удалось получить ответ от сервера");
                    return;
                }

                // Проверяем, есть ли уже такой файл в истории
                var existingResult = _checkHistory.FirstOrDefault(r => 
                    r.FilePath.Equals(filePath, StringComparison.OrdinalIgnoreCase));

                if (existingResult != null)
                {
                    // Обновляем существующую запись
                    existingResult.Users.Clear();
                    foreach (var user in users)
                    {
                        existingResult.Users.Add(user);
                    }
                    existingResult.CheckedAt = DateTime.Now;
                }
                else
                {
                    // Добавляем новую запись
                    var result = new FileCheckResult
                    {
                        FilePath = filePath,
                        CheckedAt = DateTime.Now
                    };
                    
                    foreach (var user in users)
                    {
                        result.Users.Add(user);
                    }
                    
                    _checkHistory.Insert(0, result); // Добавляем в начало списка
                }

                UpdateUI();
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка: {ex.Message}");
            }
        }

        private void UpdateUI()
        {
            LoadingPanel.Visibility = Visibility.Collapsed;
            ErrorPanel.Visibility = Visibility.Collapsed;
            
            if (_checkHistory.Count == 0)
            {
                FileTreeView.Visibility = Visibility.Collapsed;
                NoHistoryPanel.Visibility = Visibility.Visible;
                RefreshLastButton.IsEnabled = false;
            }
            else
            {
                FileTreeView.Visibility = Visibility.Visible;
                NoHistoryPanel.Visibility = Visibility.Collapsed;
                RefreshLastButton.IsEnabled = true;
            }
            
            FileCountText.Text = $"Проверено файлов: {_checkHistory.Count}";
        }

        private void ShowLoading()
        {
            LoadingPanel.Visibility = Visibility.Visible;
            FileTreeView.Visibility = Visibility.Collapsed;
            NoHistoryPanel.Visibility = Visibility.Collapsed;
            ErrorPanel.Visibility = Visibility.Collapsed;
        }

        private void ShowError(string message)
        {
            LoadingPanel.Visibility = Visibility.Collapsed;
            FileTreeView.Visibility = Visibility.Collapsed;
            NoHistoryPanel.Visibility = Visibility.Collapsed;
            ErrorPanel.Visibility = Visibility.Visible;
            
            ErrorMessage.Text = message;
        }

        private async void RefreshLast_Click(object sender, RoutedEventArgs e)
        {
            if (_checkHistory.Count > 0)
            {
                var lastFile = _checkHistory.First();
                await AddFileToHistoryAsync(lastFile.FilePath);
            }
        }

        private void ClearHistory_Click(object sender, RoutedEventArgs e)
        {
            if (_checkHistory.Count == 0)
                return;

            var result = MessageBox.Show(
                "Вы уверены, что хотите очистить историю проверок?",
                "Очистка истории",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _checkHistory.Clear();
                UpdateUI();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void SendMessage_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as Button;
                if (button?.Tag is FileUserInfo userInfo)
                {
                    // Получаем путь к файлу из родительского FileCheckResult
                    var fileResult = _checkHistory.FirstOrDefault(r => r.Users.Contains(userInfo));
                    if (fileResult == null)
                        return;

                    var currentUserName = Environment.UserName;
                    var message = $"Пользователь {currentUserName} просит сохранить и закрыть файл {fileResult.FilePath}";

                    // Отправляем сообщение через msg.exe на компьютер пользователя
                    var processInfo = new ProcessStartInfo
                    {
                        FileName = "msg.exe",
                        Arguments = $"* /server:{userInfo.ClientName} \"{message}\"",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };

                    using (var process = Process.Start(processInfo))
                    {
                        process?.WaitForExit(5000); // Ждем максимум 5 секунд
                        
                        if (process?.ExitCode == 0)
                        {
                            MessageBox.Show(
                                $"Сообщение успешно отправлено пользователю {userInfo.UserName} на компьютер {userInfo.ClientName}",
                                "Сообщение отправлено",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);
                        }
                        else
                        {
                            var error = process?.StandardError.ReadToEnd();
                            MessageBox.Show(
                                $"Не удалось отправить сообщение.\n\nВозможные причины:\n" +
                                $"- Компьютер {userInfo.ClientName} недоступен\n" +
                                $"- Служба сообщений отключена на удаленном компьютере\n" +
                                $"- Недостаточно прав для отправки сообщений\n\n" +
                                $"Технические детали: {error}",
                                "Ошибка отправки",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Произошла ошибка при отправке сообщения:\n\n{ex.Message}\n\n" +
                    $"Убедитесь, что:\n" +
                    $"- У вас есть права администратора\n" +
                    $"- Служба 'Messenger' запущена на обоих компьютерах\n" +
                    $"- Компьютер получателя доступен в сети",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}
