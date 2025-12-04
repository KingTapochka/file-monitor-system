# Скрипт удаления FileMonitorService
# Запускать от имени администратора

param(
    [string]$ServiceName = "FileMonitorService",
    [string]$InstallPath = "C:\Services\FileMonitorService"
)

Write-Host "=== Удаление FileMonitorService ===" -ForegroundColor Cyan

# Проверка прав администратора
if (-NOT ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")) {
    Write-Error "Требуются права администратора!"
    exit 1
}

# Остановка службы
$service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($service) {
    if ($service.Status -eq "Running") {
        Write-Host "Остановка службы..." -ForegroundColor Yellow
        Stop-Service -Name $ServiceName -Force
        Start-Sleep -Seconds 2
    }
    
    Write-Host "Удаление службы..." -ForegroundColor Yellow
    sc.exe delete $ServiceName
    Start-Sleep -Seconds 2
}
else {
    Write-Host "Служба не найдена" -ForegroundColor Yellow
}

# Удаление файлов
if (Test-Path $InstallPath) {
    Write-Host "Удаление файлов из $InstallPath..." -ForegroundColor Yellow
    Remove-Item $InstallPath -Recurse -Force
}

Write-Host "✓ Удаление завершено" -ForegroundColor Green
