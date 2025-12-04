using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using SharpShell.Attributes;
using SharpShell.SharpContextMenu;
using FileMonitorClient.Services;
using FileMonitorClient.UI;

namespace FileMonitorClient
{
    /// <summary>
    /// Context Menu Handler для отображения пункта "Кто использует файл?"
    /// </summary>
    [ComVisible(true)]
    [COMServerAssociation(AssociationType.AllFiles)]
    public class FileMonitorContextMenu : SharpContextMenu
    {
        /// <summary>
        /// Определяет, показывать ли пункт меню
        /// </summary>
        protected override bool CanShowMenu()
        {
            // Показываем только для одного файла
            return SelectedItemPaths.Count() == 1;
        }

        /// <summary>
        /// Создает контекстное меню
        /// </summary>
        protected override ContextMenuStrip CreateMenu()
        {
            var menu = new ContextMenuStrip();

            var mainItem = new ToolStripMenuItem
            {
                Text = "Кто использует файл?",
                Image = Properties.Resources.FileUserIcon // Добавим иконку позже
            };

            mainItem.Click += (sender, args) => OnMenuItemClick();

            menu.Items.Add(mainItem);

            return menu;
        }

        /// <summary>
        /// Обработчик клика на пункт меню
        /// </summary>
        private void OnMenuItemClick()
        {
            try
            {
                var filePath = SelectedItemPaths.First();

                // Создаем и показываем окно с пользователями
                var dialog = new FileUsersDialog(filePath);
                dialog.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Ошибка при получении информации о файле:\n{ex.Message}",
                    "Ошибка",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
    }
}
