# File Monitor System - MSI Installer

MSI установщик для File Monitor System с поддержкой установки серверных и клиентских компонентов.

## Возможности

- ✅ Выбор компонентов при установке (сервер/клиент/оба)
- ✅ Автоматическая установка Windows Service (сервер)
- ✅ Автоматическая регистрация Shell Extension (клиент)
- ✅ Удаление службы при деинсталляции
- ✅ Отмена регистрации Shell Extension при удалении
- ✅ Поддержка upgrade (обновление версий)
- ✅ Права администратора

## Требования для сборки

### 1. Установка WiX Toolset v4

```powershell
# Через dotnet CLI
dotnet tool install --global wix

# Или скачайте с официального сайта
# https://wixtoolset.org/
```

### 2. Установка .NET 8 SDK

```powershell
winget install Microsoft.DotNet.SDK.8
```

## Сборка установщика

### Шаг 1: Сборка проектов

```powershell
# Вернитесь в корень решения
cd c:\monitoring_polzovateley

# Сборка серверной части
cd FileMonitorService
dotnet publish -c Release -r win-x64 --self-contained false -o bin\Release\net8.0-windows\publish

# Сборка клиентской части
cd ..\FileMonitorClient
dotnet build -c Release

cd ..
```

### Шаг 2: Сборка MSI

```powershell
cd Installer

# Сборка установщика
dotnet build -c Release

# Результат будет в: bin\Release\FileMonitorSetup.msi
```

### Быстрая команда (все в одном)

```powershell
# Из корня проекта
.\Scripts\Build-Installer.ps1
```

## Установка

### Через GUI

1. Запустите `FileMonitorSetup.msi` от имени администратора
2. Выберите компоненты для установки:
   - **Серверная служба** - для файлового сервера
   - **Клиентское расширение** - для клиентских компьютеров
3. Выберите путь установки (по умолчанию: `C:\Program Files\File Monitor System`)
4. Нажмите "Install"

### Через командную строку

```powershell
# Установка обоих компонентов
msiexec /i FileMonitorSetup.msi /qn

# Установка только сервера
msiexec /i FileMonitorSetup.msi ADDLOCAL=ServerFeature /qn

# Установка только клиента
msiexec /i FileMonitorSetup.msi ADDLOCAL=ClientFeature /qn

# Установка с логированием
msiexec /i FileMonitorSetup.msi /l*v install.log

# Тихая установка с интерфейсом прогресса
msiexec /i FileMonitorSetup.msi /qb
```

## Удаление

### Через GUI

1. Откройте "Программы и компоненты" (appwiz.cpl)
2. Найдите "File Monitor System"
3. Нажмите "Удалить"
4. Следуйте инструкциям

### Через командную строку

```powershell
# Получить ProductCode
$productCode = Get-WmiObject -Class Win32_Product | Where-Object { $_.Name -eq "File Monitor System" } | Select-Object -ExpandProperty IdentifyingNumber

# Удаление
msiexec /x $productCode /qn

# Или по имени MSI
msiexec /x FileMonitorSetup.msi /qn
```

## Что происходит при установке

### Серверный компонент:

1. Копирование файлов в `C:\Program Files\File Monitor System\Service\`
2. Создание Windows Service через `sc create`
3. Запуск службы `sc start FileMonitorService`
4. Создание записей в реестре

### Клиентский компонент:

1. Копирование файлов в `C:\Program Files\File Monitor System\Client\`
2. Регистрация COM через `regasm` (32-bit)
3. Регистрация COM через `regasm` (64-bit)
4. Перезапуск Windows Explorer
5. Создание записей в реестре

## Что происходит при удалении

### Серверный компонент:

1. Остановка службы `sc stop`
2. Удаление службы `sc delete`
3. Удаление файлов
4. Очистка реестра

### Клиентский компонент:

1. Отмена регистрации COM (32-bit и 64-bit)
2. Удаление файлов
3. Перезапуск Windows Explorer (опционально)
4. Очистка реестра

## Структура установщика

```
Installer/
├── FileMonitorInstaller.wixproj    # Проект WiX
├── Product.wxs                      # Главный файл продукта
├── ServerComponent.wxs              # Определение серверных компонентов
├── ClientComponent.wxs              # Определение клиентских компонентов
└── README.md                        # Эта документация
```

## Настройка

### Изменение версии

В файле `Product.wxs`:

```xml
<?define ProductVersion="1.0.0.0" ?>
```

### Изменение производителя

```xml
<?define Manufacturer="Your Company" ?>
```

### Изменение UpgradeCode

⚠️ **Важно:** НЕ меняйте UpgradeCode после первого релиза! Это нарушит обновления.

```xml
<?define UpgradeCode="{12345678-1234-1234-1234-123456789012}" ?>
```

### Изменение путей установки

В `Product.wxs` в секции `<StandardDirectory>`:

```xml
<Directory Id="INSTALLFOLDER" Name="Your Folder Name">
```

## Распространение

### Подписывание MSI (рекомендуется)

```powershell
# Используйте signtool для подписи
signtool sign /f certificate.pfx /p password /t http://timestamp.digicert.com FileMonitorSetup.msi
```

### Создание .exe установщика (опционально)

Используйте WiX Burn для создания bundle:

```powershell
# Создайте Bundle.wxs
# dotnet build для создания setup.exe
```

## Развертывание через Group Policy

1. Создайте сетевую папку для MSI
2. Откройте Group Policy Management
3. Computer Configuration → Policies → Software Settings → Software Installation
4. Добавьте MSI пакет
5. Выберите "Assigned" для автоматической установки

## Массовое развертывание через PowerShell

```powershell
# Скрипт для массовой установки на серверах
$servers = Get-Content servers.txt

foreach ($server in $servers) {
    Invoke-Command -ComputerName $server -ScriptBlock {
        msiexec /i "\\share\FileMonitorSetup.msi" ADDLOCAL=ServerFeature /qn
    }
}

# Скрипт для массовой установки на клиентах
$clients = Get-ADComputer -Filter * | Select-Object -ExpandProperty Name

foreach ($client in $clients) {
    Invoke-Command -ComputerName $client -ScriptBlock {
        msiexec /i "\\share\FileMonitorSetup.msi" ADDLOCAL=ClientFeature /qn
    }
}
```

## Отладка

### Включение подробного логирования

```powershell
msiexec /i FileMonitorSetup.msi /l*v install.log
```

### Просмотр ошибок установки

```powershell
# Откройте Event Viewer
eventvwr.msc

# Проверьте Application Log для ошибок MsiInstaller
```

### Проверка регистрации компонентов

```powershell
# Проверка службы
Get-Service FileMonitorService

# Проверка Shell Extension в реестре
reg query "HKCR\*\shellex\ContextMenuHandlers" /s | findstr "FileMonitor"

# Проверка установленных файлов
dir "C:\Program Files\File Monitor System" -Recurse
```

## Известные проблемы

### Shell Extension не появляется после установки

**Решение:**
1. Перезагрузите компьютер
2. Или вручную перезапустите Explorer: `taskkill /f /im explorer.exe ; explorer.exe`

### Служба не запускается

**Решение:**
1. Проверьте логи: `C:\Program Files\File Monitor System\Service\logs\`
2. Проверьте права службы
3. Убедитесь, что .NET 8 Runtime установлен

### Ошибка при удалении

**Решение:**
1. Вручную остановите службу: `sc stop FileMonitorService`
2. Повторите удаление
3. При необходимости вручную удалите: `sc delete FileMonitorService`

## Поддержка

При возникновении проблем:
1. Проверьте install.log
2. Проверьте Event Viewer
3. Проверьте права администратора
4. Убедитесь, что все зависимости установлены

## Лицензия

MIT License
