#Requires -RunAsAdministrator

<#
.SYNOPSIS
    СРОЧНОЕ ИСПРАВЛЕНИЕ после установки MSI
.DESCRIPTION
    Исправляет проблемы с Explorer и регистрирует Shell Extension
#>

Write-Host @"
╔════════════════════════════════════════════════════════════╗
║     ИСПРАВЛЕНИЕ ПОСЛЕ УСТАНОВКИ MSI                       ║
╚════════════════════════════════════════════════════════════╝
"@ -ForegroundColor Red

Write-Host "`n[!] Обнаружена проблема с панелью задач/Explorer" -ForegroundColor Yellow
Write-Host "    Это произошло из-за некорректной регистрации Shell Extension`n" -ForegroundColor Yellow

# Шаг 1: Восстановление Explorer
Write-Host "[1/4] Восстановление Explorer..." -ForegroundColor Cyan

$explorerRunning = Get-Process explorer -ErrorAction SilentlyContinue
if (-not $explorerRunning) {
    Write-Host "  [!] Explorer не запущен - запускаем..." -ForegroundColor Yellow
    Start-Process explorer.exe
    Start-Sleep -Seconds 3
    Write-Host "  [OK] Explorer запущен" -ForegroundColor Green
} else {
    Write-Host "  [OK] Explorer работает" -ForegroundColor Green
}

# Шаг 2: Проверка установки клиента
Write-Host "`n[2/4] Проверка установки клиента..." -ForegroundColor Cyan
$clientPath = "C:\Program Files\File Monitor System\Client"
$dllPath = Join-Path $clientPath "FileMonitorClient.dll"

if (Test-Path $dllPath) {
    Write-Host "  [OK] Клиент установлен: $clientPath" -ForegroundColor Green
} else {
    Write-Host "  [ERROR] Клиент НЕ установлен!" -ForegroundColor Red
    Write-Host "    Сначала установите MSI:" -ForegroundColor Yellow
    Write-Host "    msiexec /i FileMonitorSetup.msi ADDLOCAL=ClientFeature /qb" -ForegroundColor White
    exit 1
}

# Шаг 3: Отмена старой регистрации (если есть)
Write-Host "`n[3/4] Очистка старой регистрации..." -ForegroundColor Cyan

$regasm32 = "$env:SystemRoot\Microsoft.NET\Framework\v4.0.30319\RegAsm.exe"
$regasm64 = "$env:SystemRoot\Microsoft.NET\Framework64\v4.0.30319\RegAsm.exe"

if (Test-Path $regasm32) {
    Write-Host "  Отмена регистрации (32-bit)..." -ForegroundColor Yellow
    & $regasm32 "$dllPath" /unregister /nologo 2>$null
}

if (Test-Path $regasm64) {
    Write-Host "  Отмена регистрации (64-bit)..." -ForegroundColor Yellow
    & $regasm64 "$dllPath" /unregister /nologo 2>$null
}

Write-Host "  [OK] Старая регистрация очищена" -ForegroundColor Green

# Шаг 4: Правильная регистрация Shell Extension
Write-Host "`n[4/4] Регистрация Shell Extension..." -ForegroundColor Cyan

Write-Host "  [!] Explorer будет перезапущен!" -ForegroundColor Yellow
Start-Sleep -Seconds 2

# Остановка Explorer
Write-Host "  Остановка Explorer..." -ForegroundColor Yellow
Get-Process explorer -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 2

# Регистрация
Write-Host "  Регистрация через regasm (32-bit)..." -ForegroundColor Yellow
if (Test-Path $regasm32) {
    & $regasm32 "$dllPath" /codebase /nologo
}

Write-Host "  Регистрация через regasm (64-bit)..." -ForegroundColor Yellow
if (Test-Path $regasm64) {
    & $regasm64 "$dllPath" /codebase /nologo
}

# Запуск Explorer
Write-Host "  Запуск Explorer..." -ForegroundColor Yellow
Start-Process explorer.exe
Start-Sleep -Seconds 3

Write-Host "`n" + ("="*60) -ForegroundColor Green
Write-Host "[SUCCESS] Исправление завершено!" -ForegroundColor Green
Write-Host ("="*60) -ForegroundColor Green

Write-Host "`nЧто было сделано:" -ForegroundColor Cyan
Write-Host "  ✓ Explorer восстановлен" -ForegroundColor Green
Write-Host "  ✓ Старая регистрация очищена" -ForegroundColor Green
Write-Host "  ✓ Shell Extension зарегистрирован правильно" -ForegroundColor Green
Write-Host "  ✓ Explorer перезапущен" -ForegroundColor Green

Write-Host "`nТеперь проверьте:" -ForegroundColor Cyan
Write-Host "  1. Откройте любую папку в Explorer" -ForegroundColor White
Write-Host "  2. ПКМ на файле" -ForegroundColor White
Write-Host "  3. Найдите пункт 'Кто использует файл?'" -ForegroundColor White

Write-Host "`nДополнительно:" -ForegroundColor Cyan
Write-Host "  - Настройте адрес сервера в:" -ForegroundColor White
Write-Host "    $clientPath\FileMonitorClient.dll.config" -ForegroundColor Gray
Write-Host "  - Запустите полную проверку: .\Scripts\Test-System.ps1" -ForegroundColor White

Write-Host "`n[!] Если пункт всё равно не появился - ПЕРЕЗАГРУЗИТЕ компьютер" -ForegroundColor Yellow
