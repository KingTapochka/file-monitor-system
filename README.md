# File Monitor - Система отслеживания использования файлов

Полное решение для мониторинга открытых файлов на Windows Server с клиентским расширением для Windows Explorer.

## 📋 Компоненты решения

### 1. FileMonitorService (Серверная часть)
Windows Service на .NET 8 для мониторинга открытых файлов через SMB.

**Возможности:**
- Мониторинг через PowerShell `Get-SmbOpenFile`
- REST API для запросов
- In-memory кэширование
- Автоматическое обновление каждые 10 секунд
- Логирование через Serilog

**Порты:**
- HTTP: `5000`
- HTTPS: `5001`

### 2. FileMonitorClient (Клиентская часть)
Shell Extension на .NET Framework 4.8 для интеграции с Windows Explorer.

**Возможности:**
- Контекстное меню "Кто использует файл?"
- WPF окно с таблицей пользователей
- HTTP клиент для связи с сервером
- Индикатор загрузки

## 🚀 Быстрый старт

### Вариант 1: Установка через MSI (Рекомендуется)

```powershell
# 1. Сборка установщика
.\Scripts\Build-Installer.ps1

# 2. Установка ОБОИХ компонентов
msiexec /i "Installer\FileMonitorSetup.msi"

# 3. Установка ТОЛЬКО сервера (для файлового сервера)
msiexec /i "Installer\FileMonitorSetup.msi" ADDLOCAL=ServerFeature /qb

# 4. Установка ТОЛЬКО клиента (для пользовательских машин)
msiexec /i "Installer\FileMonitorSetup.msi" ADDLOCAL=ClientFeature /qb

# Удаление
msiexec /x "Installer\FileMonitorSetup.msi" /qn
```

### Вариант 2: Установка через PowerShell скрипты

**Серверная часть:**

```powershell
# Установка
.\Scripts\Install-Service.ps1

# Проверка
Invoke-RestMethod -Uri "http://localhost:5000/api/files/health"

# Удаление
.\Scripts\Uninstall-Service.ps1
```

**Клиентская часть:**

```powershell
# Установка
.\Scripts\Install-Client.ps1 -Force

# Удаление
.\Scripts\Uninstall-Client.ps1 -Force
```

## 📖 Использование

1. **Откройте файл** на сетевой папке с другого компьютера
2. **Нажмите правой кнопкой** на файле
3. **Выберите** "Кто использует файл?"
4. **Увидите список** пользователей с информацией:
   - Имя пользователя
   - Компьютер
   - Режим доступа (Read/Write)
   - Время открытия

## 🏗️ Архитектура

```
┌─────────────────────────────────────────┐
│     Windows Server 2019                  │
│  ┌───────────────────────────────────┐  │
│  │  FileMonitorService               │  │
│  │  - Get-SmbOpenFile                │  │
│  │  - REST API (5000/5001)           │  │
│  │  - In-memory Cache                │  │
│  └───────────────────────────────────┘  │
└─────────────────────────────────────────┘
              ↕ HTTPS
┌─────────────────────────────────────────┐
│     Windows 10/11 (Клиент)              │
│  ┌───────────────────────────────────┐  │
│  │  FileMonitorClient                │  │
│  │  - Shell Extension                │  │
│  │  - Context Menu Handler           │  │
│  │  - WPF UI                         │  │
│  └───────────────────────────────────┘  │
└─────────────────────────────────────────┘
```

## 🔧 Конфигурация

### FileMonitorService (appsettings.json)

```json
{
  "FileMonitor": {
    "RefreshIntervalSeconds": 10,
    "CacheExpirationMinutes": 5
  },
  "Kestrel": {
    "Endpoints": {
      "Http": { "Url": "http://localhost:5000" },
      "Https": { "Url": "https://localhost:5001" }
    }
  }
}
```

### FileMonitorClient

Измените адрес API в `FileMonitorApiClient.cs`:

```csharp
public FileMonitorApiClient(string baseUrl = "http://your-server:5000")
```

## 📡 API Endpoints

### GET /api/files/users?filePath={путь}
Получить пользователей файла.

### GET /api/files/active
Получить все активные файлы.

### GET /api/files/user/{userName}
Получить файлы пользователя.

### POST /api/files/refresh
Принудительно обновить кэш.

### GET /api/files/health
Проверка работоспособности.

## 🛠️ Требования

### Для сборки установщика
- .NET 8 SDK
- WiX Toolset 4.0.4 (`dotnet tool install --global wix`)
- PowerShell 5.1+

### Серверная часть
- Windows Server 2016+ или Windows 10+
- .NET 8.0 Runtime
- PowerShell 5.1+
- SMB File Server
- Права администратора

### Клиентская часть
- Windows 10/11
- .NET Framework 4.8
- Права администратора (только для установки)

## 📝 Логирование

### Сервер
Логи в папке `logs/service-YYYYMMDD.txt`

### Клиент
Ошибки отображаются в UI, также можно проверить Event Viewer.

## ⚠️ Известные ограничения

1. **Get-SmbOpenFile** работает только на Windows Server с SMB
2. Требуются **права администратора** для службы
3. **Локальные файлы** (не SMB) не отслеживаются
4. Shell Extension работает только для **одного файла** за раз

## 🐛 Устранение неполадок

### Служба не запускается
```powershell
# Проверка логов
Get-Content C:\Services\FileMonitorService\logs\service-*.txt -Tail 50

# Проверка прав
whoami /priv
```

### Пункт меню не появляется
```powershell
# Проверка регистрации
reg query HKCR\*\shellex\ContextMenuHandlers

# Переустановка
srm uninstall FileMonitorClient.dll
srm install FileMonitorClient.dll -codebase
taskkill /f /im explorer.exe ; explorer.exe
```

### Ошибка подключения к API
```powershell
# Проверка доступности
Test-NetConnection localhost -Port 5000

# Проверка службы
sc query FileMonitorService

# Проверка firewall
netsh advfirewall firewall show rule name="FileMonitorService"
```

## 🔒 Безопасность

- Используйте **HTTPS** в продакшене
- Настройте **Windows Authentication** для API
- Ограничьте доступ к портам через **Firewall**
- Регулярно обновляйте зависимости

## 📦 Структура проекта

```
monitoring_polzovateley/
├── FileMonitorService/           # Серверная часть (.NET 8)
│   ├── Controllers/              # REST API контроллеры
│   ├── Models/                   # Модели данных
│   ├── Services/                 # Бизнес-логика
│   ├── Program.cs                # Точка входа
│   └── appsettings.json          # Конфигурация
├── FileMonitorClient/            # Клиентская часть (.NET Framework 4.8)
│   ├── Models/                   # API модели
│   ├── Services/                 # HTTP клиент
│   ├── UI/                       # WPF интерфейс
│   └── FileMonitorContextMenu.cs # Shell Extension
├── Installer/                    # MSI установщик (WiX)
│   ├── Product.wxs               # Определение продукта
│   ├── ServerComponent.wxs       # Серверные компоненты
│   ├── ClientComponent.wxs       # Клиентские компоненты
│   └── README.md                 # Документация установщика
├── Scripts/                      # PowerShell скрипты
│   ├── Install-Service.ps1       # Установка сервера
│   ├── Install-Client.ps1        # Установка клиента
│   ├── Build-Installer.ps1       # Сборка MSI
│   └── Test-Api.ps1              # Тестирование API
├── README.md                     # Основная документация
└── DEPLOYMENT.md                 # Руководство по развертыванию
```

## 🚦 Статус разработки

- ✅ Серверная служба (Windows Service)
- ✅ Мониторинг через Get-SmbOpenFile
- ✅ REST API (5 endpoints)
- ✅ In-memory кэширование
- ✅ Shell Extension (SharpShell)
- ✅ WPF UI с красивым дизайном
- ✅ MSI установщик с выбором компонентов
- ✅ PowerShell скрипты установки
- ✅ Автоматическое удаление при деинсталляции
- ⏳ Тестирование на Windows 10/11/Server 2019+

## 📄 Лицензия

MIT License

## 👥 Автор

Проект создан для мониторинга файлового сервера Windows Server 2019.
