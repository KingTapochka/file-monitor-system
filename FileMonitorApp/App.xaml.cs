using System;
using System.Windows;
using System.IO;
using FileMonitorApp.Services;

namespace FileMonitorApp
{
    public partial class App : Application
    {
        private TrayIconManager? _trayManager;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Проверяем, не запущено ли уже приложение
            var currentProcess = System.Diagnostics.Process.GetCurrentProcess();
            var processes = System.Diagnostics.Process.GetProcessesByName(currentProcess.ProcessName);
            if (processes.Length > 1)
            {
                MessageBox.Show("File Monitor уже запущен!", "File Monitor", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                Shutdown();
                return;
            }

            // Инициализация системного трея
            _trayManager = new TrayIconManager();
            _trayManager.Initialize();

            // Если передан файл через командную строку (SendTo)
            if (e.Args.Length > 0 && File.Exists(e.Args[0]))
            {
                _trayManager.CheckFile(e.Args[0]);
            }

            // Не показываем главное окно при запуске - только трей
            MainWindow = null;
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _trayManager?.Dispose();
            base.OnExit(e);
        }
    }
}
