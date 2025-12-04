# File Monitor System - Быстрый старт

## Установка

### Вариант 1: PowerShell установка (Рекомендуется)

1. **Запустите PowerShell от имени администратора**
   - Правый клик на PowerShell -> "Запуск от имени администратора"

2. **Перейдите в папку проекта**
   ```powershell
   cd C:\monitoring_polzovateley
   ```

3. **Установите нужные компоненты**

   **Для файлового сервера:**
   ```powershell
   .\Scripts\Install-All.ps1 -Component Server
   ```

   **Для рабочей станции:**
   ```powershell
   .\Scripts\Install-All.ps1 -Component Client
   ```

   **Оба компонента (с выбором):**
   ```powershell
   .\Scripts\Install-All.ps1
   ```

### Вариант 2: MSI установщик (Для массового развертывания)

1. **Установите WiX Toolset**
   ```powershell
   dotnet tool install --global wix --version 4.0.4
   ```

2. **Соберите MSI**
   ```powershell
   .\Scripts\Build-Installer.ps1
   ```

3. **Установите через MSI**
   ```powershell
   # Установка сервера
   msiexec /i Installer\bin\Release\FileMonitorSetup.msi ADDLOCAL=ServerFeature

   # Установка клиента
   msiexec /i Installer\bin\Release\FileMonitorSetup.msi ADDLOCAL=ClientFeature
   ```

## Проверка установки

### Проверка сервера
```powershell
# Проверка службы
Get-Service FileMonitorService

# Проверка API
Invoke-RestMethod http://localhost:5000/api/files/health
```

### Проверка клиента
1. Откройте любую папку в Explorer
2. Правый клик на файле
3. Должен появиться пункт "Кто использует файл?"

## Удаление

```powershell
.\Scripts\Uninstall-All.ps1
```

## Возможные проблемы

### Ошибка "Требуются права администратора"
- Запустите PowerShell от имени администратора

### Ошибка "execution policy"
```powershell
Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope Process -Force
```

### Служба не запускается
```powershell
# Проверка логов
Get-EventLog -LogName Application -Source FileMonitorService -Newest 10
```

### Shell Extension не появляется
1. Перезапустите Explorer: `taskkill /f /im explorer.exe ; Start-Process explorer.exe`
2. Проверьте регистрацию: `Get-ChildItem HKLM:\Software\Classes\*\shellex\*` | `Where-Object { $_.GetValue('') -like '*FileMonitor*' }`

## Конфигурация

### Изменение порта API (сервер)
Отредактируйте `C:\Program Files\File Monitor System\Service\appsettings.json`

### Изменение адреса API (клиент)
Отредактируйте `C:\Program Files\File Monitor System\Client\FileMonitorClient.dll.config`

## Развертывание через GPO

См. подробную документацию в `DEPLOYMENT.md`
