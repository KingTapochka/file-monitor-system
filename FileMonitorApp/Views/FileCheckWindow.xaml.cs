using System;
using System.IO;
using System.Windows;
using Microsoft.Win32;

namespace FileMonitorApp.Views
{
    public partial class FileCheckWindow : Window
    {
        public event EventHandler<string>? FileSelected;

        public FileCheckWindow()
        {
            InitializeComponent();
            FilePathTextBox.Focus();
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Выберите файл для проверки",
                Filter = "Все файлы (*.*)|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                FilePathTextBox.Text = dialog.FileName;
            }
        }

        private void TextBox_PreviewDragOver(object sender, DragEventArgs e)
        {
            e.Handled = true;
            e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) 
                ? DragDropEffects.Copy 
                : DragDropEffects.None;
        }

        private void TextBox_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Length > 0)
                {
                    FilePathTextBox.Text = files[0];
                }
            }
        }

        private void CheckButton_Click(object sender, RoutedEventArgs e)
        {
            var path = FilePathTextBox.Text.Trim();
            
            if (string.IsNullOrEmpty(path))
            {
                MessageBox.Show("Укажите путь к файлу", "Ошибка", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!File.Exists(path))
            {
                MessageBox.Show("Файл не найден", "Ошибка", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            FileSelected?.Invoke(this, path);
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
