#Requires -RunAsAdministrator

<#
.SYNOPSIS
    Комплексная проверка системы File Monitor
#>

Write-Host @"
╔════════════════════════════════════════════════════════════╗
║     FILE MONITOR SYSTEM - ТЕСТ РАБОТОСПОСОБНОСТИ          ║
╚════════════════════════════════════════════════════════════╝
"@ -ForegroundColor Cyan

# Проверка 1: Файлы установлены
Write-Host "`n[1/6] Проверка установленных файлов..." -ForegroundColor Yellow
$serverPath = "C:\Program Files\File Monitor System\Service"
$clientPath = "C:\Program Files\File Monitor System\Client"

$serverFiles = @(
    "FileMonitorService.exe",
    "FileMonitorService.dll",
    "appsettings.json"
)

$clientFiles = @(
    "FileMonitorClient.dll",
    "SharpShell.dll",
    "Newtonsoft.Json.dll"
)

$allFilesOk = $true
foreach ($file in $serverFiles) {
    $path = Join-Path $serverPath $file
    if (Test-Path $path) {
        Write-Host "  ✓ $file" -ForegroundColor Green
    } else {
        Write-Host "  ✗ $file НЕ НАЙДЕН" -ForegroundColor Red
        $allFilesOk = $false
    }
}

foreach ($file in $clientFiles) {
    $path = Join-Path $clientPath $file
    if (Test-Path $path) {
        Write-Host "  ✓ $file" -ForegroundColor Green
    } else {
        Write-Host "  ✗ $file НЕ НАЙДЕН" -ForegroundColor Red
        $allFilesOk = $false
    }
}

if ($allFilesOk) {
    Write-Host "  [OK] Все файлы на месте" -ForegroundColor Green
} else {
    Write-Host "  [FAIL] Некоторые файлы отсутствуют" -ForegroundColor Red
}

# Проверка 2: Служба
Write-Host "`n[2/6] Проверка службы Windows..." -ForegroundColor Yellow
$service = Get-Service -Name "FileMonitorService" -ErrorAction SilentlyContinue
if ($service) {
    Write-Host "  ✓ Служба найдена: $($service.DisplayName)" -ForegroundColor Green
    Write-Host "    Статус: $($service.Status)" -ForegroundColor $(if($service.Status -eq 'Running'){'Green'}else{'Yellow'})
    Write-Host "    Тип запуска: $($service.StartType)" -ForegroundColor Cyan
    
    if ($service.Status -ne 'Running') {
        Write-Host "  [!] Служба остановлена. Запускаем..." -ForegroundColor Yellow
        try {
            Start-Service -Name "FileMonitorService"
            Start-Sleep -Seconds 3
            $service = Get-Service -Name "FileMonitorService"
            if ($service.Status -eq 'Running') {
                Write-Host "  [OK] Служба запущена успешно" -ForegroundColor Green
            } else {
                Write-Host "  [FAIL] Не удалось запустить службу" -ForegroundColor Red
            }
        }
        catch {
            Write-Host "  [ERROR] $($_.Exception.Message)" -ForegroundColor Red
        }
    }
} else {
    Write-Host "  ✗ Служба НЕ установлена" -ForegroundColor Red
}

# Проверка 3: API сервера
Write-Host "`n[3/6] Проверка Web API..." -ForegroundColor Yellow
Start-Sleep -Seconds 2
try {
    $response = Invoke-RestMethod -Uri "http://localhost:5000/api/files" -Method Get -TimeoutSec 5
    Write-Host "  ✓ API доступен" -ForegroundColor Green
    Write-Host "    URL: http://localhost:5000/api/files" -ForegroundColor Cyan
    Write-Host "    Найдено открытых файлов: $($response.Count)" -ForegroundColor Cyan
}
catch {
    Write-Host "  ✗ API недоступен: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "    Проверьте логи в: $serverPath\logs" -ForegroundColor Yellow
}

# Проверка 4: Логи
Write-Host "`n[4/6] Проверка логов..." -ForegroundColor Yellow
$logsPath = Join-Path $serverPath "logs"
if (Test-Path $logsPath) {
    $logFiles = Get-ChildItem $logsPath -Filter "*.txt" | Sort-Object LastWriteTime -Descending | Select-Object -First 3
    if ($logFiles) {
        Write-Host "  ✓ Найдено лог-файлов: $($logFiles.Count)" -ForegroundColor Green
        foreach ($log in $logFiles) {
            Write-Host "    - $($log.Name) ($([math]::Round($log.Length/1KB, 2)) KB) - $($log.LastWriteTime)" -ForegroundColor Cyan
        }
        
        # Показать последние строки
        $latestLog = $logFiles[0].FullName
        Write-Host "`n  Последние записи из $($logFiles[0].Name):" -ForegroundColor Cyan
        Get-Content $latestLog -Tail 5 | ForEach-Object {
            Write-Host "    $_" -ForegroundColor Gray
        }
    } else {
        Write-Host "  [!] Лог-файлы не найдены (служба еще не писала логи)" -ForegroundColor Yellow
    }
} else {
    Write-Host "  [!] Папка логов не существует" -ForegroundColor Yellow
}

# Проверка 5: Shell Extension
Write-Host "`n[5/6] Проверка Shell Extension..." -ForegroundColor Yellow

# Поиск в реестре
$clsidPaths = @(
    "HKLM:\SOFTWARE\Classes\CLSID",
    "HKLM:\SOFTWARE\WOW6432Node\Classes\CLSID"
)

$found = $false
foreach ($path in $clsidPaths) {
    if (Test-Path $path) {
        $keys = Get-ChildItem $path -Recurse -ErrorAction SilentlyContinue | Where-Object {
            $name = $_.GetValue("") -as [string]
            $name -and $name -like "*FileMonitor*"
        }
        if ($keys) {
            $found = $true
            Write-Host "  ✓ Shell Extension зарегистрировано в $path" -ForegroundColor Green
            break
        }
    }
}

if (-not $found) {
    Write-Host "  ✗ Shell Extension НЕ зарегистрировано" -ForegroundColor Red
    Write-Host "    Запустите: .\Scripts\Register-ShellExtension.ps1" -ForegroundColor Yellow
}

# Проверка 6: Контекстное меню
Write-Host "`n[6/6] Инструкция для проверки контекстного меню..." -ForegroundColor Yellow
Write-Host @"
  Чтобы проверить работу Shell Extension:
  
  1. Откройте любую папку в Windows Explorer
  2. Создайте тестовый файл (например, test.txt)
  3. Щелкните ПРАВОЙ кнопкой мыши на файле
  4. Найдите пункт "Кто использует файл?" в контекстном меню
  5. Должно появиться окно со списком пользователей
  
  Если пункт не появился:
  - Запустите: .\Scripts\Register-ShellExtension.ps1
  - Перезапустите Explorer (будет сделано автоматически)
"@ -ForegroundColor Cyan

# Итоги
Write-Host "`n" + ("="*60) -ForegroundColor Cyan
Write-Host "РЕЗЮМЕ:" -ForegroundColor White
Write-Host ("="*60) -ForegroundColor Cyan

if ($allFilesOk) {
    Write-Host "✓ Файлы: установлены" -ForegroundColor Green
} else {
    Write-Host "✗ Файлы: проблемы с установкой" -ForegroundColor Red
}

if ($service -and $service.Status -eq 'Running') {
    Write-Host "✓ Служба: запущена и работает" -ForegroundColor Green
} elseif ($service) {
    Write-Host "⚠ Служба: установлена но не запущена" -ForegroundColor Yellow
} else {
    Write-Host "✗ Служба: не установлена" -ForegroundColor Red
}

if ($found) {
    Write-Host "✓ Shell Extension: зарегистрировано" -ForegroundColor Green
} else {
    Write-Host "✗ Shell Extension: не зарегистрировано" -ForegroundColor Red
}

Write-Host "`nДля дополнительной информации:" -ForegroundColor Cyan
Write-Host "  - Swagger UI: http://localhost:5000/swagger" -ForegroundColor White
Write-Host "  - Логи службы: $serverPath\logs" -ForegroundColor White
Write-Host "  - Регистрация клиента: .\Scripts\Register-ShellExtension.ps1" -ForegroundColor White

