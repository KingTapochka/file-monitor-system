# File Monitor - Система отслеживания использования файлов

Решение для мониторинга открытых файлов на Windows Server с клиентским приложением.

## 📋 Компоненты

### 1. FileMonitorService (Серверная часть)
Windows Service на .NET 8 для мониторинга открытых файлов через SMB.

**Возможности:**
- Мониторинг через PowerShell `Get-SmbOpenFile`
- REST API для запросов (порт 5000)
- In-memory кэширование
- Автоматическое обновление каждые 10 секунд
- DNS резолвинг IP адресов в имена компьютеров

### 2. FileMonitorApp (Клиентская часть)
WPF приложение в системном трее для проверки файлов.

**Возможности:**
- Иконка в системном трее
- Горячая клавиша Win+Shift+F
- Drag & Drop файлов
- Автозапуск при входе в систему

## 🚀 Установка через MSI

### Сборка установщика

```powershell
.\Scripts\Build-Installer.ps1
```

### Установка

```powershell
# Полная установка (сервер + клиент)
msiexec /i "Installer\FileMonitorSetup.msi"

# Только сервер (для файлового сервера)
msiexec /i "Installer\FileMonitorSetup.msi" ADDLOCAL=ServerFeature /qb

# Только клиент (для рабочих станций)
msiexec /i "Installer\FileMonitorSetup.msi" ADDLOCAL=ClientFeature /qb

# Удаление
msiexec /x "Installer\FileMonitorSetup.msi" /qn
```

## 📖 Использование

1. **Нажмите Win+Shift+F** или дважды кликните на иконку в трее
2. **Укажите путь** к файлу или перетащите файл в окно
3. **Увидите список** пользователей, открывших файл

## 🏗️ Архитектура

```
┌─────────────────────────────────────────┐
│     Windows Server 2019                  │
│  ┌───────────────────────────────────┐  │
│  │  FileMonitorService               │  │
│  │  - Get-SmbOpenFile                │  │
│  │  - REST API (порт 5000)           │  │
│  │  - In-memory Cache                │  │
│  └───────────────────────────────────┘  │
└─────────────────────────────────────────┘
              ↕ HTTP
┌─────────────────────────────────────────┐
│     Windows 10/11 (Клиент)              │
│  ┌───────────────────────────────────┐  │
│  │  FileMonitorApp                   │  │
│  │  - Системный трей                 │  │
│  │  - Win+Shift+F                    │  │
│  │  - WPF UI                         │  │
│  └───────────────────────────────────┘  │
└─────────────────────────────────────────┘
```

## 🔧 Конфигурация

### Серверная часть (appsettings.json)

```json
{
  "ServerSettings": {
    "Port": 5000,
    "ServerName": "TS03"
  },
  "FileMonitor": {
    "RefreshIntervalSeconds": 10,
    "CacheExpirationMinutes": 5
  },
  "ShareMappings": [
    { "ShareName": "Share", "LocalPath": "D:\\Share" }
  ]
}
```

### Клиентская часть

Адрес сервера настраивается в приложении:
- Правый клик на иконку → Настройки
- Введите адрес: `http://сервер:5000`

## 📡 API Endpoints

| Метод | URL | Описание |
|-------|-----|----------|
| GET | /api/files/users?filePath={путь} | Получить пользователей файла |
| GET | /api/files/active | Получить все активные файлы |
| GET | /api/files/user/{userName} | Получить файлы пользователя |
| POST | /api/files/refresh | Принудительно обновить кэш |
| GET | /api/files/health | Проверка работоспособности |
| GET | /api/files/debug | Диагностика |

## 🛠️ Требования

### Серверная часть
- Windows Server 2016+ или Windows 10+
- .NET 8.0 Runtime (встроен в exe)
- PowerShell 5.1+
- SMB File Server
- Права администратора

### Клиентская часть
- Windows 10/11
- .NET 8.0 Runtime
- Сеть к серверу (порт 5000)

## 📦 Структура проекта

```
monitoring_polzovateley/
├── FileMonitorService/      # Серверная часть (.NET 8)
│   ├── Controllers/         # REST API контроллеры
│   ├── Models/             # Модели данных
│   ├── Services/           # Бизнес-логика
│   └── appsettings.json    # Конфигурация
├── FileMonitorApp/         # Клиент WPF (.NET 8)
│   ├── Views/              # XAML окна
│   ├── Services/           # API клиент
│   └── Models/             # Модели
├── Installer/              # MSI установщик (WiX 4)
│   ├── Product.wxs         # Определение продукта
│   ├── ServerComponent.wxs # Серверные компоненты
│   └── ClientComponent.wxs # Клиентские компоненты
└── Scripts/                # PowerShell скрипты
    ├── Build-Installer.ps1 # Сборка MSI
    ├── Install-Service.ps1 # Установка сервера
    ├── Diagnose-Server.ps1 # Диагностика
    └── Test-Api.ps1        # Тестирование API
```

## 🔒 Особенности

- **Single-file deployment** для сервера (~100MB exe)
- **Автозапуск** службы и клиента после установки
- **Обход прокси** в клиенте для прямого подключения
- **DNS кэширование** для резолвинга IP → hostname

## 📄 Лицензия

MIT License

## 📌 Версия

v3.0
