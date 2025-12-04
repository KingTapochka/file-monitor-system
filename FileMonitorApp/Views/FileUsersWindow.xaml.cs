using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using FileMonitorApp.Models;
using FileMonitorApp.Services;

namespace FileMonitorApp.Views
{
    public partial class FileUsersWindow : Window
    {
        private readonly string _filePath;
        private readonly ApiClient _apiClient;

        public FileUsersWindow(string filePath)
        {
            InitializeComponent();
            _filePath = filePath;
            _apiClient = new ApiClient();
            
            FilePathText.Text = filePath;
            FilePathText.ToolTip = filePath;
            
            Loaded += FileUsersWindow_Loaded;
        }

        private async void FileUsersWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadFileUsersAsync();
        }

        private async Task LoadFileUsersAsync()
        {
            ShowLoading();
            
            try
            {
                var users = await _apiClient.GetFileUsersAsync(_filePath);
                
                if (users == null)
                {
                    ShowError("Не удалось получить ответ от сервера");
                    return;
                }
                
                if (users.Count == 0)
                {
                    ShowNoUsers();
                }
                else
                {
                    ShowUsers(users);
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
            UsersDataGrid.Visibility = Visibility.Collapsed;
            NoUsersPanel.Visibility = Visibility.Collapsed;
            ErrorPanel.Visibility = Visibility.Collapsed;
            
            RefreshButton.IsEnabled = false;
        }

        private void ShowUsers(List<FileUserInfo> users)
        {
            LoadingPanel.Visibility = Visibility.Collapsed;
            UsersDataGrid.Visibility = Visibility.Visible;
            NoUsersPanel.Visibility = Visibility.Collapsed;
            ErrorPanel.Visibility = Visibility.Collapsed;
            
            UsersDataGrid.ItemsSource = users;
            
            RefreshButton.IsEnabled = true;
        }

        private void ShowNoUsers()
        {
            LoadingPanel.Visibility = Visibility.Collapsed;
            UsersDataGrid.Visibility = Visibility.Collapsed;
            NoUsersPanel.Visibility = Visibility.Visible;
            ErrorPanel.Visibility = Visibility.Collapsed;
            
            RefreshButton.IsEnabled = true;
        }

        private void ShowError(string message)
        {
            LoadingPanel.Visibility = Visibility.Collapsed;
            UsersDataGrid.Visibility = Visibility.Collapsed;
            NoUsersPanel.Visibility = Visibility.Collapsed;
            ErrorPanel.Visibility = Visibility.Visible;
            
            ErrorMessage.Text = message;
            
            RefreshButton.IsEnabled = true;
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadFileUsersAsync();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
