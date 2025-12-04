# Скрипт установки FileMonitorClient Shell Extension
# Запускать от имени администратора

param(
    [string]$ProjectPath = "$PSScriptRoot\..\FileMonitorClient",
    [switch]$Force
)

Write-Host "=== Установка FileMonitorClient Shell Extension ===" -ForegroundColor Cyan

# Проверка прав администратора
if (-NOT ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")) {
    Write-Error "Требуются права администратора!"
    exit 1
}

# Сборка проекта
Write-Host "Сборка проекта..." -ForegroundColor Yellow
Push-Location $ProjectPath
try {
    dotnet build -c Release
    if ($LASTEXITCODE -ne 0) {
        throw "Ошибка при сборке проекта"
    }
}
finally {
    Pop-Location
}

# Путь к DLL
$dllPath = Join-Path $ProjectPath "bin\Release\net48\FileMonitorClient.dll"
if (-not (Test-Path $dllPath)) {
    Write-Error "DLL не найдена: $dllPath"
    exit 1
}

Write-Host "Найдена DLL: $dllPath" -ForegroundColor Green

# Остановка Explorer
if ($Force) {
    Write-Host "Остановка Windows Explorer..." -ForegroundColor Yellow
    taskkill /f /im explorer.exe 2>$null
    Start-Sleep -Seconds 2
}

# Регистрация через regasm
Write-Host "Регистрация Shell Extension (32-bit)..." -ForegroundColor Yellow
$regasm32 = "$env:WINDIR\Microsoft.NET\Framework\v4.0.30319\RegAsm.exe"
if (Test-Path $regasm32) {
    & $regasm32 $dllPath /codebase /nologo
}

Write-Host "Регистрация Shell Extension (64-bit)..." -ForegroundColor Yellow
$regasm64 = "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\RegAsm.exe"
if (Test-Path $regasm64) {
    & $regasm64 $dllPath /codebase /nologo
}

# Запуск Explorer
if ($Force) {
    Write-Host "Запуск Windows Explorer..." -ForegroundColor Yellow
    Start-Process explorer.exe
    Start-Sleep -Seconds 2
}

Write-Host "`n✓ Shell Extension установлена!" -ForegroundColor Green
Write-Host "`nТеперь в контекстном меню файлов появится пункт 'Кто использует файл?'" -ForegroundColor Cyan
Write-Host "`nЕсли пункт не появился:" -ForegroundColor Yellow
Write-Host "  1. Перезапустите Explorer: taskkill /f /im explorer.exe ; explorer.exe" -ForegroundColor Gray
Write-Host "  2. Перезагрузите компьютер" -ForegroundColor Gray
Write-Host "  3. Проверьте логи в Event Viewer" -ForegroundColor Gray

Write-Host "`n=== Установка завершена ===" -ForegroundColor Cyan
