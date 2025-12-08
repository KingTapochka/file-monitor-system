#Requires -RunAsAdministrator
<#
.SYNOPSIS
    Скачивает и устанавливает Sysinternals Handle.exe для полного мониторинга файлов
.DESCRIPTION
    Handle.exe от Sysinternals позволяет видеть ВСЕ открытые файлы,
    включая те которые не блокируются через SMB (например Adobe Acrobat)
.EXAMPLE
    .\Install-Handle.ps1
#>

$ErrorActionPreference = "Stop"

Write-Host "=== Установка Sysinternals Handle ===" -ForegroundColor Cyan
Write-Host ""

# Путь к сервису
$servicePath = "C:\Program Files\FileMonitor\Service"
$tempPath = "$env:TEMP\handle.zip"
$downloadUrl = "https://download.sysinternals.com/files/Handle.zip"

# Создаём папку если нет
if (-not (Test-Path $servicePath)) {
    Write-Host "Создание папки $servicePath..." -ForegroundColor Gray
    New-Item -Path $servicePath -ItemType Directory -Force | Out-Null
}

# Скачиваем
Write-Host "Скачивание Handle.zip..." -ForegroundColor Gray
try {
    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
    Invoke-WebRequest -Uri $downloadUrl -OutFile $tempPath -UseBasicParsing
    Write-Host "✓ Скачано" -ForegroundColor Green
} catch {
    Write-Host "✗ Ошибка скачивания: $_" -ForegroundColor Red
    exit 1
}

# Распаковываем
Write-Host "Распаковка..." -ForegroundColor Gray
try {
    Expand-Archive -Path $tempPath -DestinationPath "$env:TEMP\handle" -Force
    
    # Копируем нужную версию (64-bit)
    $handleExe = "$env:TEMP\handle\handle64.exe"
    if (Test-Path $handleExe) {
        Copy-Item $handleExe -Destination $servicePath -Force
        Write-Host "✓ handle64.exe скопирован в $servicePath" -ForegroundColor Green
    } else {
        # Пробуем обычный handle.exe
        $handleExe = "$env:TEMP\handle\handle.exe"
        if (Test-Path $handleExe) {
            Copy-Item $handleExe -Destination $servicePath -Force
            Write-Host "✓ handle.exe скопирован в $servicePath" -ForegroundColor Green
        } else {
            throw "handle.exe не найден в архиве"
        }
    }
} catch {
    Write-Host "✗ Ошибка распаковки: $_" -ForegroundColor Red
    exit 1
}

# Очистка
Remove-Item $tempPath -Force -ErrorAction SilentlyContinue
Remove-Item "$env:TEMP\handle" -Recurse -Force -ErrorAction SilentlyContinue

# Принимаем лицензию Sysinternals (запускаем один раз)
Write-Host "Принятие лицензии Sysinternals..." -ForegroundColor Gray
$handlePath = Join-Path $servicePath "handle64.exe"
if (-not (Test-Path $handlePath)) {
    $handlePath = Join-Path $servicePath "handle.exe"
}

try {
    Start-Process $handlePath -ArgumentList "-accepteula" -Wait -NoNewWindow
    Write-Host "✓ Лицензия принята" -ForegroundColor Green
} catch {
    Write-Host "Предупреждение: не удалось запустить handle.exe" -ForegroundColor Yellow
}

# Перезапускаем сервис если он установлен
$service = Get-Service -Name "FileMonitorService" -ErrorAction SilentlyContinue
if ($service) {
    Write-Host "Перезапуск FileMonitorService..." -ForegroundColor Gray
    Restart-Service -Name "FileMonitorService" -Force
    Write-Host "✓ Сервис перезапущен" -ForegroundColor Green
}

Write-Host ""
Write-Host "=== Установка завершена ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "Handle.exe установлен. Теперь FileMonitorService сможет видеть" -ForegroundColor Gray
Write-Host "все открытые файлы, включая PDF открытые в Adobe Acrobat." -ForegroundColor Gray
