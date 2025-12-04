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
            FilePathPanel.Visibility = Visibility.Visible;
        }

        private void DropZone_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
                DropZone.BorderBrush = System.Windows.Media.Brushes.Green;
                DropHint.Visibility = Visibility.Visible;
                FilePathPanel.Visibility = Visibility.Collapsed;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void DropZone_DragLeave(object sender, DragEventArgs e)
        {
            DropZone.BorderBrush = (System.Windows.Media.Brush)FindResource("PrimaryBrush");
            DropHint.Visibility = Visibility.Collapsed;
            FilePathPanel.Visibility = Visibility.Visible;
        }

        private void DropZone_Drop(object sender, DragEventArgs e)
        {
            DropZone.BorderBrush = (System.Windows.Media.Brush)FindResource("PrimaryBrush");
            
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Length > 0 && File.Exists(files[0]))
                {
                    FileSelected?.Invoke(this, files[0]);
                    Close();
                }
            }
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
