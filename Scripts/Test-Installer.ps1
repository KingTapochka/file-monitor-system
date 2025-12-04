# Скрипт тестирования MSI установщика
# Проверяет корректность установки и удаления компонентов

param(
    [ValidateSet("Server", "Client", "Both")]
    [string]$Component = "Both",
    
    [switch]$SkipUninstall
)

$ErrorActionPreference = "Continue"

Write-Host "=== Тестирование File Monitor MSI Installer ===" -ForegroundColor Cyan
Write-Host "Компонент: $Component`n" -ForegroundColor White

# Поиск MSI файла
$msiPath = $null
$possiblePaths = @(
    "$PSScriptRoot\..\Installer\bin\Release\FileMonitorSetup.msi",
    "$PSScriptRoot\..\Installer\FileMonitorSetup.msi"
)

foreach ($path in $possiblePaths) {
    if (Test-Path $path) {
        $msiPath = $path
        break
    }
}

if (-not $msiPath) {
    Write-Error "MSI файл не найден! Сначала выполните: .\Scripts\Build-Installer.ps1"
    exit 1
}

Write-Host "✓ MSI найден: $msiPath`n" -ForegroundColor Green

# Функция проверки прав администратора
function Test-Admin {
    $currentUser = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($currentUser)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

if (-not (Test-Admin)) {
    Write-Error "Требуются права администратора!"
    exit 1
}

# Определение параметров установки
$installParams = switch ($Component) {
    "Server" { "ADDLOCAL=ServerFeature" }
    "Client" { "ADDLOCAL=ClientFeature" }
    "Both" { "" }
}

# ТЕСТ 1: Установка
Write-Host "[ТЕСТ 1] Установка компонента: $Component" -ForegroundColor Yellow
Write-Host "Команда: msiexec /i `"$msiPath`" $installParams /qb" -ForegroundColor Gray

$process = Start-Process msiexec -ArgumentList "/i `"$msiPath`" $installParams /qb /l*v `"$env:TEMP\install.log`"" -Wait -PassThru

if ($process.ExitCode -eq 0) {
    Write-Host "✓ Установка завершена (код: $($process.ExitCode))" -ForegroundColor Green
}
else {
    Write-Host "✗ Ошибка установки (код: $($process.ExitCode))" -ForegroundColor Red
    Write-Host "Лог: $env:TEMP\install.log" -ForegroundColor Yellow
}

Start-Sleep -Seconds 3

# ТЕСТ 2: Проверка серверного компонента
if ($Component -in @("Server", "Both")) {
    Write-Host "`n[ТЕСТ 2] Проверка серверного компонента" -ForegroundColor Yellow
    
    # Проверка файлов
    $serverPath = "C:\Program Files\File Monitor System\Service"
    if (Test-Path "$serverPath\FileMonitorService.exe") {
        Write-Host "  ✓ Файлы сервера установлены" -ForegroundColor Green
    }
    else {
        Write-Host "  ✗ Файлы сервера НЕ найдены" -ForegroundColor Red
    }
    
    # Проверка службы
    Start-Sleep -Seconds 5
    $service = Get-Service -Name "FileMonitorService" -ErrorAction SilentlyContinue
    if ($service) {
        Write-Host "  ✓ Служба зарегистрирована (статус: $($service.Status))" -ForegroundColor Green
        
        if ($service.Status -eq "Running") {
            Write-Host "  ✓ Служба работает" -ForegroundColor Green
            
            # Проверка API
            Start-Sleep -Seconds 3
            try {
                $response = Invoke-RestMethod -Uri "http://localhost:5000/api/files/health" -TimeoutSec 5
                Write-Host "  ✓ API доступен" -ForegroundColor Green
            }
            catch {
                Write-Host "  ✗ API недоступен: $($_.Exception.Message)" -ForegroundColor Red
            }
        }
        else {
            Write-Host "  ⚠ Служба не запущена" -ForegroundColor Yellow
        }
    }
    else {
        Write-Host "  ✗ Служба НЕ зарегистрирована" -ForegroundColor Red
    }
    
    # Проверка реестра
    $regPath = "HKLM:\SOFTWARE\FileMonitorSystem"
    if (Test-Path $regPath) {
        $serverReg = Get-ItemProperty $regPath -Name "ServerPath" -ErrorAction SilentlyContinue
        if ($serverReg) {
            Write-Host "  ✓ Запись в реестре создана" -ForegroundColor Green
        }
    }
    else {
        Write-Host "  ✗ Запись в реестре НЕ найдена" -ForegroundColor Red
    }
}

# ТЕСТ 3: Проверка клиентского компонента
if ($Component -in @("Client", "Both")) {
    Write-Host "`n[ТЕСТ 3] Проверка клиентского компонента" -ForegroundColor Yellow
    
    # Проверка файлов
    $clientPath = "C:\Program Files\File Monitor System\Client"
    if (Test-Path "$clientPath\FileMonitorClient.dll") {
        Write-Host "  ✓ Файлы клиента установлены" -ForegroundColor Green
    }
    else {
        Write-Host "  ✗ Файлы клиента НЕ найдены" -ForegroundColor Red
    }
    
    # Проверка регистрации COM
    $shellExtKey = "Registry::HKEY_CLASSES_ROOT\*\shellex\ContextMenuHandlers"
    $found = $false
    
    try {
        $handlers = Get-ChildItem $shellExtKey -ErrorAction SilentlyContinue
        foreach ($handler in $handlers) {
            if ($handler.PSChildName -like "*FileMonitor*") {
                $found = $true
                break
            }
        }
    }
    catch {
        Write-Host "  ⚠ Не удается проверить регистрацию COM" -ForegroundColor Yellow
    }
    
    if ($found) {
        Write-Host "  ✓ Shell Extension зарегистрирована" -ForegroundColor Green
    }
    else {
        Write-Host "  ✗ Shell Extension НЕ зарегистрирована" -ForegroundColor Red
        Write-Host "    (Может потребоваться перезагрузка)" -ForegroundColor Gray
    }
    
    # Проверка реестра
    $regPath = "HKLM:\SOFTWARE\FileMonitorSystem"
    if (Test-Path $regPath) {
        $clientReg = Get-ItemProperty $regPath -Name "ClientPath" -ErrorAction SilentlyContinue
        if ($clientReg) {
            Write-Host "  ✓ Запись в реестре создана" -ForegroundColor Green
        }
    }
}

# ТЕСТ 4: Удаление (если не пропущено)
if (-not $SkipUninstall) {
    Write-Host "`n[ТЕСТ 4] Удаление компонента" -ForegroundColor Yellow
    Write-Host "Ожидание 5 секунд перед удалением..." -ForegroundColor Gray
    Start-Sleep -Seconds 5
    
    Write-Host "Команда: msiexec /x `"$msiPath`" /qn" -ForegroundColor Gray
    $process = Start-Process msiexec -ArgumentList "/x `"$msiPath`" /qn /l*v `"$env:TEMP\uninstall.log`"" -Wait -PassThru
    
    if ($process.ExitCode -eq 0) {
        Write-Host "✓ Удаление завершено (код: $($process.ExitCode))" -ForegroundColor Green
    }
    else {
        Write-Host "✗ Ошибка удаления (код: $($process.ExitCode))" -ForegroundColor Red
        Write-Host "Лог: $env:TEMP\uninstall.log" -ForegroundColor Yellow
    }
    
    Start-Sleep -Seconds 3
    
    # Проверка удаления сервера
    if ($Component -in @("Server", "Both")) {
        Write-Host "`n  Проверка удаления сервера:" -ForegroundColor Cyan
        
        $service = Get-Service -Name "FileMonitorService" -ErrorAction SilentlyContinue
        if (-not $service) {
            Write-Host "    ✓ Служба удалена" -ForegroundColor Green
        }
        else {
            Write-Host "    ✗ Служба НЕ удалена" -ForegroundColor Red
        }
        
        if (-not (Test-Path "C:\Program Files\File Monitor System\Service")) {
            Write-Host "    ✓ Файлы сервера удалены" -ForegroundColor Green
        }
        else {
            Write-Host "    ✗ Файлы сервера остались" -ForegroundColor Red
        }
    }
    
    # Проверка удаления клиента
    if ($Component -in @("Client", "Both")) {
        Write-Host "`n  Проверка удаления клиента:" -ForegroundColor Cyan
        
        if (-not (Test-Path "C:\Program Files\File Monitor System\Client")) {
            Write-Host "    ✓ Файлы клиента удалены" -ForegroundColor Green
        }
        else {
            Write-Host "    ✗ Файлы клиента остались" -ForegroundColor Red
        }
    }
}
else {
    Write-Host "`n[ТЕСТ 4] Удаление пропущено (-SkipUninstall)" -ForegroundColor Yellow
    Write-Host "Для удаления вручную выполните:" -ForegroundColor Cyan
    Write-Host "  msiexec /x `"$msiPath`" /qn" -ForegroundColor Gray
}

# Итоги
Write-Host "`n=== Тестирование завершено ===" -ForegroundColor Cyan
Write-Host "`nЛоги:" -ForegroundColor Yellow
if (Test-Path "$env:TEMP\install.log") {
    Write-Host "  Установка: $env:TEMP\install.log" -ForegroundColor Gray
}
if (Test-Path "$env:TEMP\uninstall.log") {
    Write-Host "  Удаление: $env:TEMP\uninstall.log" -ForegroundColor Gray
}

Write-Host "`nДля просмотра подробных логов:" -ForegroundColor Cyan
Write-Host "  Get-Content `"$env:TEMP\install.log`" -Tail 50" -ForegroundColor Gray
