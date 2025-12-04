# Скрипт удаления FileMonitorClient Shell Extension
# Запускать от имени администратора

param(
    [string]$ProjectPath = "$PSScriptRoot\..\FileMonitorClient",
    [switch]$Force
)

Write-Host "=== Удаление FileMonitorClient Shell Extension ===" -ForegroundColor Cyan

# Проверка прав администратора
if (-NOT ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")) {
    Write-Error "Требуются права администратора!"
    exit 1
}

# Путь к DLL
$dllPath = Join-Path $ProjectPath "bin\Release\net48\FileMonitorClient.dll"
if (-not (Test-Path $dllPath)) {
    Write-Warning "DLL не найдена: $dllPath"
}

# Остановка Explorer
if ($Force) {
    Write-Host "Остановка Windows Explorer..." -ForegroundColor Yellow
    taskkill /f /im explorer.exe 2>$null
    Start-Sleep -Seconds 2
}

# Отмена регистрации
if (Test-Path $dllPath) {
    Write-Host "Отмена регистрации Shell Extension (32-bit)..." -ForegroundColor Yellow
    $regasm32 = "$env:WINDIR\Microsoft.NET\Framework\v4.0.30319\RegAsm.exe"
    if (Test-Path $regasm32) {
        & $regasm32 $dllPath /unregister /nologo
    }

    Write-Host "Отмена регистрации Shell Extension (64-bit)..." -ForegroundColor Yellow
    $regasm64 = "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\RegAsm.exe"
    if (Test-Path $regasm64) {
        & $regasm64 $dllPath /unregister /nologo
    }
}

# Запуск Explorer
if ($Force) {
    Write-Host "Запуск Windows Explorer..." -ForegroundColor Yellow
    Start-Process explorer.exe
    Start-Sleep -Seconds 2
}

Write-Host "✓ Shell Extension удалена" -ForegroundColor Green
Write-Host "`nРекомендуется перезагрузить компьютер для полного удаления." -ForegroundColor Yellow
