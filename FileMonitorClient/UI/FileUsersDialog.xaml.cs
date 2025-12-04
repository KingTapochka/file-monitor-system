using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using FileMonitorClient.Models;
using FileMonitorClient.Services;

namespace FileMonitorClient.UI
{
    /// <summary>
    /// Окно для отображения пользователей файла
    /// </summary>
    public partial class FileUsersDialog : Window
    {
        private readonly string _filePath;
        private readonly FileMonitorApiClient _apiClient;

        public FileUsersDialog(string filePath)
        {
            InitializeComponent();
            
            _filePath = filePath;
            _apiClient = new FileMonitorApiClient();

            // Устанавливаем путь к файлу в UI
            FilePathTextBlock.Text = Path.GetFileName(filePath);
            FilePathTextBlock.ToolTip = filePath;

            // Загружаем данные
            Loaded += async (s, e) => await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            try
            {
                ShowLoading();

                // Проверка доступности сервиса
                var isHealthy = await _apiClient.CheckHealthAsync();
                if (!isHealthy)
                {
                    ShowError("Не удается подключиться к службе мониторинга.\nУбедитесь, что FileMonitorService запущена.");
                    return;
                }

                // Получение данных о пользователях
                var response = await _apiClient.GetFileUsersAsync(_filePath);

                if (response == null || response.Users.Count == 0)
                {
                    ShowNoUsers();
                }
                else
                {
                    ShowUsers(response);
                }
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка при получении данных:\n{ex.Message}");
            }
        }

        private void ShowLoading()
        {
            LoadingPanel.Visibility = Visibility.Visible;
            UsersDataGrid.Visibility = Visibility.Collapsed;
            NoUsersPanel.Visibility = Visibility.Collapsed;
            ErrorPanel.Visibility = Visibility.Collapsed;
            LastUpdatedTextBlock.Text = string.Empty;
        }

        private void ShowUsers(FileUsersResponse response)
        {
            LoadingPanel.Visibility = Visibility.Collapsed;
            UsersDataGrid.Visibility = Visibility.Visible;
            NoUsersPanel.Visibility = Visibility.Collapsed;
            ErrorPanel.Visibility = Visibility.Collapsed;

            UsersDataGrid.ItemsSource = response.Users;
            
            Title = $"Кто использует файл? ({response.UserCount} пользователей)";
            LastUpdatedTextBlock.Text = $"Обновлено: {response.LastUpdated:HH:mm:ss}";
        }

        private void ShowNoUsers()
        {
            LoadingPanel.Visibility = Visibility.Collapsed;
            UsersDataGrid.Visibility = Visibility.Collapsed;
            NoUsersPanel.Visibility = Visibility.Visible;
            ErrorPanel.Visibility = Visibility.Collapsed;

            Title = "Кто использует файл? (0 пользователей)";
            LastUpdatedTextBlock.Text = $"Обновлено: {DateTime.Now:HH:mm:ss}";
        }

        private void ShowError(string message)
        {
            LoadingPanel.Visibility = Visibility.Collapsed;
            UsersDataGrid.Visibility = Visibility.Collapsed;
            NoUsersPanel.Visibility = Visibility.Collapsed;
            ErrorPanel.Visibility = Visibility.Visible;

            ErrorMessageTextBlock.Text = message;
            Title = "Кто использует файл? (Ошибка)";
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadDataAsync();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            _apiClient?.Dispose();
            base.OnClosed(e);
        }
    }
}
