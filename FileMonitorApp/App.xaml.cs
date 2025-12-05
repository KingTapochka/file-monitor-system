using System;
using System.Windows;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using FileMonitorApp.Services;

namespace FileMonitorApp
{
    public partial class App : Application
    {
        private TrayIconManager? _trayManager;
        private Mutex? _mutex;
        private CancellationTokenSource? _pipeServerCts;
        private const string MUTEX_NAME = "FileMonitorApp_SingleInstance";
        private const string PIPE_NAME = "FileMonitorApp_Pipe";

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Проверяем единственность экземпляра через Mutex
            _mutex = new Mutex(true, MUTEX_NAME, out bool createdNew);

            if (!createdNew)
            {
                // Приложение уже запущено - передаем файл через pipe
                if (e.Args.Length > 0 && File.Exists(e.Args[0]))
                {
                    SendFileToRunningInstance(e.Args[0]);
                }
                Shutdown();
                return;
            }

            // Запускаем сервер для приема файлов от других экземпляров
            StartPipeServer();

            // Инициализация системного трея
            _trayManager = new TrayIconManager();
            _trayManager.Initialize();

            // Если передан файл через командную строку
            if (e.Args.Length > 0 && File.Exists(e.Args[0]))
            {
                _trayManager.CheckFile(e.Args[0]);
            }

            // Не показываем главное окно при запуске - только трей
            MainWindow = null;
        }

        private void SendFileToRunningInstance(string filePath)
        {
            try
            {
                using var client = new NamedPipeClientStream(".", PIPE_NAME, PipeDirection.Out);
                client.Connect(3000); // Таймаут 3 секунды
                
                using var writer = new StreamWriter(client);
                writer.WriteLine(filePath);
                writer.Flush();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Не удалось отправить файл: {ex.Message}");
            }
        }

        private void StartPipeServer()
        {
            _pipeServerCts = new CancellationTokenSource();
            
            Task.Run(async () =>
            {
                while (!_pipeServerCts.Token.IsCancellationRequested)
                {
                    try
                    {
                        using var server = new NamedPipeServerStream(PIPE_NAME, PipeDirection.In);
                        await server.WaitForConnectionAsync(_pipeServerCts.Token);
                        
                        using var reader = new StreamReader(server);
                        var filePath = await reader.ReadLineAsync();
                        
                        if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
                        {
                            Dispatcher.Invoke(() => _trayManager?.CheckFile(filePath));
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Ошибка pipe сервера: {ex.Message}");
                    }
                }
            }, _pipeServerCts.Token);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _pipeServerCts?.Cancel();
            _trayManager?.Dispose();
            _mutex?.ReleaseMutex();
            _mutex?.Dispose();
            base.OnExit(e);
        }
    }
}
