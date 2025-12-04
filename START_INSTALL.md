# УСТАНОВКА FILE MONITOR SYSTEM

## Быстрая установка (3 шага)

### Шаг 1: Откройте PowerShell от имени администратора
- Нажмите Win + X
- Выберите "Windows PowerShell (администратор)"

### Шаг 2: Перейдите в папку проекта
```powershell
cd C:\monitoring_polzovateley
```

### Шаг 3: Запустите установщик
```powershell
.\Scripts\Install-All.ps1
```

## Что установить?

После запуска скрипт спросит, что установить:

**1 - Серверная служба** (для файлового сервера)
- Windows Service с REST API
- Мониторинг открытых файлов через Get-SmbOpenFile
- Кеширование результатов

**2 - Клиентское расширение** (для рабочих станций)
- Shell Extension в контекстном меню
- WPF окно с информацией о пользователях
- Подключение к серверу по HTTP

**3 - Оба компонента** (для тестирования на одной машине)

## Проверка после установки

### Если установили СЕРВЕР:
```powershell
# Проверка службы
Get-Service FileMonitorService

# Проверка API
Invoke-RestMethod http://localhost:5000/api/files/health
```

### Если установили КЛИЕНТ:
1. Откройте любую папку в Explorer
2. Правой кнопкой на любом файле
3. Должен появиться пункт "Кто использует файл?"

## Удаление

```powershell
.\Scripts\Uninstall-All.ps1
```

## Возможные проблемы

❌ **"Не удается загрузить файл... политика выполнения"**
```powershell
Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope Process -Force
```

❌ **"Требуются права администратора"**
- Закройте PowerShell
- Запустите заново от имени администратора (Win+X -> "Windows PowerShell (администратор)")

❌ **Служба не запускается**
```powershell
# Посмотрите логи
Get-EventLog -LogName Application -Source FileMonitorService -Newest 10
```

❌ **Shell Extension не появляется в меню**
```powershell
# Перезапустите Explorer
taskkill /f /im explorer.exe
Start-Process explorer.exe
```

## Настройка

### Изменить порт сервера:
Файл: `C:\Program Files\File Monitor System\Service\appsettings.json`

### Изменить адрес API для клиента:
Файл: `C:\Program Files\File Monitor System\Client\FileMonitorClient.dll.config`

## Контакты

Если возникли проблемы, проверьте файл QUICKSTART.md для детальной информации.
