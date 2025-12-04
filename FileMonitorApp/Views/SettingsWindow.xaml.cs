using System;
using System.Windows;
using FileMonitorApp.Services;

namespace FileMonitorApp.Views
{
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
            LoadSettings();
        }

        private void LoadSettings()
        {
            ServerAddressTextBox.Text = ConfigManager.ServerAddress;
            ApiKeyTextBox.Text = ConfigManager.ApiKey;
        }

        private async void TestConnectionButton_Click(object sender, RoutedEventArgs e)
        {
            var serverAddress = ServerAddressTextBox.Text.Trim();
            
            if (string.IsNullOrEmpty(serverAddress))
            {
                ShowConnectionStatus(false, "Укажите адрес сервера");
                MessageBox.Show("Укажите адрес сервера", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Нормализуем адрес (добавляем http:// если нужно)
            serverAddress = NormalizeServerAddress(serverAddress);
            ServerAddressTextBox.Text = serverAddress; // Обновляем в поле ввода

            // Валидация формата адреса
            if (!ValidateServerAddress(serverAddress, out var errorMessage))
            {
                ShowConnectionStatus(false, errorMessage);
                MessageBox.Show(errorMessage, "Ошибка формата", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Показываем статус "Проверка..."
            ShowConnectionStatus(null, "Проверка подключения...");
            
            // Принудительно обновляем UI
            await System.Windows.Threading.Dispatcher.Yield();
            
            try
            {
                using var client = new ApiClient(serverAddress, ApiKeyTextBox.Text.Trim());
                var (success, message) = await client.CheckHealthAsync();
                
                ShowConnectionStatus(success, message);
                
                // Показываем MessageBox с результатом
                if (success)
                {
                    MessageBox.Show("✓ Подключение к серверу успешно!\n\nСервер отвечает на запросы.", 
                        "Тест подключения", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show($"✗ Не удалось подключиться к серверу\n\n{message}\n\nПроверьте:\n• Служба FileMonitorService запущена на сервере\n• Порт 5000 открыт в файрволе\n• Адрес сервера указан правильно", 
                        "Тест подключения", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                var errorMsg = $"Ошибка: {ex.Message}";
                ShowConnectionStatus(false, errorMsg);
                MessageBox.Show($"✗ Ошибка подключения\n\n{ex.Message}", 
                    "Тест подключения", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string NormalizeServerAddress(string address)
        {
            // Убираем UNC-путь
            address = address.TrimStart('\\', '/');
            
            // Добавляем http:// если не указан протокол
            if (!address.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && 
                !address.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                address = "http://" + address;
            }

            return address;
        }

        private bool ValidateServerAddress(string address, out string errorMessage)
        {
            errorMessage = string.Empty;

            // Проверяем что это валидный URL
            if (!Uri.TryCreate(address, UriKind.Absolute, out var uri))
            {
                errorMessage = "Неверный формат URL";
                return false;
            }

            return true;
        }

        private void ShowConnectionStatus(bool? success, string message)
        {
            if (success == null)
            {
                ConnectionStatusIcon.Text = "🔄";
                ConnectionStatusText.Foreground = System.Windows.Media.Brushes.Gray;
            }
            else if (success.Value)
            {
                ConnectionStatusIcon.Text = "🟢";
                ConnectionStatusText.Foreground = System.Windows.Media.Brushes.Green;
            }
            else
            {
                ConnectionStatusIcon.Text = "🔴";
                ConnectionStatusText.Foreground = System.Windows.Media.Brushes.Red;
            }
            
            ConnectionStatusText.Text = message;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var serverAddress = ServerAddressTextBox.Text.Trim();
            
            if (string.IsNullOrEmpty(serverAddress))
            {
                MessageBox.Show("Укажите адрес сервера", "Ошибка", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Нормализуем адрес (добавляем http:// если нужно)
            serverAddress = NormalizeServerAddress(serverAddress);

            // Валидация формата адреса
            if (!ValidateServerAddress(serverAddress, out var errorMessage))
            {
                MessageBox.Show(errorMessage + "\n\nПример: ts03:5000", "Ошибка формата", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Сохраняем через ConfigManager (в папку пользователя)
                ConfigManager.ServerAddress = serverAddress;
                ConfigManager.ApiKey = ApiKeyTextBox.Text.Trim();
                
                MessageBox.Show("Настройки сохранены", "Успех", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения настроек: {ex.Message}", "Ошибка", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
