using FileMonitorService;
using FileMonitorService.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

// Настройка Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("logs/service-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    Log.Information("Запуск FileMonitorService");

    var builder = WebApplication.CreateBuilder(args);

    // Настройка Serilog
    builder.Host.UseSerilog();

    // Настройка Windows Service
    builder.Host.UseWindowsService();

    // Регистрация сервисов
    builder.Services.AddMemoryCache();
    builder.Services.AddSingleton<ISmbFileMonitor, SmbFileMonitor>();
    builder.Services.AddSingleton<IFileMonitorCache, FileMonitorCache>();
    
    // Добавление фонового сервиса мониторинга
    builder.Services.AddHostedService<FileMonitorWorker>();

    // Добавление Web API
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    // Настройка поведения при ошибках фоновых сервисов
    builder.Services.Configure<HostOptions>(options =>
    {
        options.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore;
    });

    var app = builder.Build();

    // Настройка HTTP pipeline
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();
    app.UseAuthorization();
    app.MapControllers();

    // Запуск приложения (Web API + Worker Service)
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Приложение завершилось с ошибкой");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}

return 0;
