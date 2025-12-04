# Скрипт установки FileMonitorService как Windows Service
# Запускать от имени администратора

param(
    [string]$ServiceName = "FileMonitorService",
    [string]$InstallPath = "C:\Services\FileMonitorService",
    [string]$ProjectPath = "$PSScriptRoot\..\FileMonitorService"
)

Write-Host "=== Установка FileMonitorService ===" -ForegroundColor Cyan

# Проверка прав администратора
if (-NOT ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")) {
    Write-Error "Требуются права администратора! Запустите PowerShell от имени администратора."
    exit 1
}

# Остановка и удаление существующей службы
Write-Host "Проверка существующей службы..." -ForegroundColor Yellow
$existingService = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($existingService) {
    Write-Host "Остановка службы $ServiceName..." -ForegroundColor Yellow
    Stop-Service -Name $ServiceName -Force -ErrorAction SilentlyContinue
    
    Write-Host "Удаление службы $ServiceName..." -ForegroundColor Yellow
    sc.exe delete $ServiceName
    Start-Sleep -Seconds 2
}

# Создание директории установки
Write-Host "Создание директории $InstallPath..." -ForegroundColor Yellow
if (Test-Path $InstallPath) {
    Remove-Item $InstallPath -Recurse -Force
}
New-Item -ItemType Directory -Path $InstallPath -Force | Out-Null

# Публикация проекта
Write-Host "Сборка и публикация проекта..." -ForegroundColor Yellow
Push-Location $ProjectPath
try {
    dotnet publish -c Release -o $InstallPath --self-contained false
    if ($LASTEXITCODE -ne 0) {
        throw "Ошибка при сборке проекта"
    }
}
finally {
    Pop-Location
}

# Создание службы
Write-Host "Создание Windows Service..." -ForegroundColor Yellow
$servicePath = Join-Path $InstallPath "FileMonitorService.exe"
sc.exe create $ServiceName binPath= "`"$servicePath`"" start= auto
sc.exe description $ServiceName "Служба мониторинга открытых файлов на SMB сервере"

# Настройка восстановления службы при сбоях
Write-Host "Настройка автоматического восстановления..." -ForegroundColor Yellow
sc.exe failure $ServiceName reset= 86400 actions= restart/60000/restart/60000/restart/60000

# Запуск службы
Write-Host "Запуск службы..." -ForegroundColor Yellow
Start-Service -Name $ServiceName

# Проверка статуса
Start-Sleep -Seconds 3
$service = Get-Service -Name $ServiceName
if ($service.Status -eq "Running") {
    Write-Host "`n✓ Служба успешно установлена и запущена!" -ForegroundColor Green
    
    # Проверка API
    Write-Host "`nПроверка доступности API..." -ForegroundColor Yellow
    Start-Sleep -Seconds 5
    try {
        $response = Invoke-RestMethod -Uri "http://localhost:5000/api/files/health" -TimeoutSec 10
        Write-Host "✓ API работает корректно!" -ForegroundColor Green
        Write-Host "  Swagger UI: https://localhost:5001/swagger" -ForegroundColor Cyan
    }
    catch {
        Write-Warning "API пока не доступен. Проверьте логи в $InstallPath\logs\"
    }
}
else {
    Write-Error "Служба не запустилась. Проверьте логи в $InstallPath\logs\"
    exit 1
}

Write-Host "`n=== Установка завершена ===" -ForegroundColor Cyan
Write-Host "Служба: $ServiceName" -ForegroundColor White
Write-Host "Путь: $InstallPath" -ForegroundColor White
Write-Host "Логи: $InstallPath\logs\" -ForegroundColor White
Write-Host "`nДля управления службой используйте:" -ForegroundColor Yellow
Write-Host "  Start-Service $ServiceName" -ForegroundColor Gray
Write-Host "  Stop-Service $ServiceName" -ForegroundColor Gray
Write-Host "  Restart-Service $ServiceName" -ForegroundColor Gray
Write-Host "  Get-Service $ServiceName" -ForegroundColor Gray
