# FileMonitorService - Серверная часть

Windows Service для мониторинга открытых файлов на файловом сервере.

## Возможности

- ✅ Мониторинг открытых файлов через Get-SmbOpenFile
- ✅ REST API для получения информации о пользователях файлов
- ✅ In-memory кэширование с автоматическим обновлением каждые 10 секунд
- ✅ Логирование через Serilog
- ✅ Swagger UI для тестирования API
- ✅ Windows Service интеграция

## Требования

- Windows Server 2016+ или Windows 10+
- .NET 8.0 Runtime
- Права администратора (для Get-SmbOpenFile)
- Настроенный SMB сервер

## API Endpoints

### GET /api/files/users?filePath={путь}
Получить список пользователей, открывших файл.

**Пример:**
```
GET /api/files/users?filePath=C:\Share\document.docx
```

**Ответ:**
```json
{
  "filePath": "C:\\Share\\document.docx",
  "users": [
    {
      "userName": "DOMAIN\\user1",
      "clientName": "PC-001",
      "filePath": "C:\\Share\\document.docx",
      "accessMode": "Read/Write",
      "openedAt": "2025-12-02T10:30:00",
      "sessionId": 12345,
      "fileId": 67890
    }
  ],
  "lastUpdated": "2025-12-02T10:35:00",
  "userCount": 1
}
```

### GET /api/files/active
Получить список всех активных файлов.

### GET /api/files/user/{userName}
Получить файлы конкретного пользователя.

### POST /api/files/refresh
Принудительно обновить кэш.

### GET /api/files/health
Проверка работоспособности сервиса.

## Установка и запуск

### Режим разработки

```powershell
cd FileMonitorService
dotnet restore
dotnet build
dotnet run
```

API будет доступно по адресу:
- HTTP: http://localhost:5000
- HTTPS: https://localhost:5001
- Swagger: https://localhost:5001/swagger

### Установка как Windows Service

```powershell
# Сборка проекта
dotnet publish -c Release -o C:\Services\FileMonitorService

# Установка службы
sc create FileMonitorService binPath="C:\Services\FileMonitorService\FileMonitorService.exe"
sc description FileMonitorService "Служба мониторинга открытых файлов"

# Запуск службы
sc start FileMonitorService

# Проверка статуса
sc query FileMonitorService
```

### Удаление службы

```powershell
sc stop FileMonitorService
sc delete FileMonitorService
```

## Конфигурация

Настройки в `appsettings.json`:

```json
{
  "FileMonitor": {
    "RefreshIntervalSeconds": 10,
    "CacheExpirationMinutes": 5,
    "EnableWindowsAuthentication": true
  },
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://localhost:5000"
      },
      "Https": {
        "Url": "https://localhost:5001"
      }
    }
  }
}
```

## Логирование

Логи сохраняются в папку `logs/`:
- `service-YYYYMMDD.txt` - ежедневные лог-файлы
- Логи также выводятся в консоль

## Тестирование

1. Откройте файл на сетевой папке
2. Выполните запрос к API:

```powershell
# PowerShell
$response = Invoke-RestMethod -Uri "http://localhost:5000/api/files/users?filePath=C:\Share\test.txt" -Method Get
$response | ConvertTo-Json
```

## Безопасность

- Служба требует прав администратора для выполнения Get-SmbOpenFile
- Рекомендуется настроить HTTPS с сертификатом
- Добавьте Windows Authentication для защиты API
- Используйте firewall для ограничения доступа к портам

## Известные ограничения

- Get-SmbOpenFile работает только на Windows Server с настроенным SMB
- Требуются права администратора
- Локальные файлы (не SMB) не отслеживаются в текущей версии

## Следующие шаги

1. ✅ Серверная часть создана
2. ⏳ Создание клиентской Shell Extension
3. ⏳ Создание UI для отображения пользователей
4. ⏳ Создание установщиков
