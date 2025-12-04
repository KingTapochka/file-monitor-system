#Requires -RunAsAdministrator

<#
.SYNOPSIS
    Регистрация Shell Extension для FileMonitorClient
.DESCRIPTION
    Использует regsvr32 для регистрации COM компонента
#>

param(
    [Parameter(Mandatory=$false)]
    [string]$ClientPath = "C:\Program Files\File Monitor System\Client"
)

Write-Host "=== Регистрация FileMonitor Shell Extension ===" -ForegroundColor Cyan

try {
    $dllPath = Join-Path $ClientPath "FileMonitorClient.dll"
    
    if (-not (Test-Path $dllPath)) {
        Write-Host "[ERROR] DLL не найдена: $dllPath" -ForegroundColor Red
        exit 1
    }

    Write-Host "[1/4] Остановка Explorer..." -ForegroundColor Yellow
    Get-Process explorer -ErrorAction SilentlyContinue | Stop-Process -Force
    Start-Sleep -Seconds 2

    Write-Host "[2/4] Регистрация через regasm (32-bit)..." -ForegroundColor Yellow
    $regasm32 = "$env:SystemRoot\Microsoft.NET\Framework\v4.0.30319\RegAsm.exe"
    if (Test-Path $regasm32) {
        & $regasm32 "$dllPath" /codebase /nologo
        Write-Host "  [OK] 32-bit регистрация выполнена" -ForegroundColor Green
    }

    Write-Host "[3/4] Регистрация через regasm (64-bit)..." -ForegroundColor Yellow
    $regasm64 = "$env:SystemRoot\Microsoft.NET\Framework64\v4.0.30319\RegAsm.exe"
    if (Test-Path $regasm64) {
        & $regasm64 "$dllPath" /codebase /nologo
        Write-Host "  [OK] 64-bit регистрация выполнена" -ForegroundColor Green
    }

    Write-Host "[4/4] Запуск Explorer..." -ForegroundColor Yellow
    Start-Process explorer.exe
    Start-Sleep -Seconds 3

    Write-Host "`n[SUCCESS] Shell Extension зарегистрировано!" -ForegroundColor Green
    Write-Host "`nТеперь попробуйте:" -ForegroundColor Cyan
    Write-Host "  1. Откройте любую папку в Explorer" -ForegroundColor White
    Write-Host "  2. Щелкните правой кнопкой на любом файле" -ForegroundColor White
    Write-Host "  3. Найдите пункт 'Кто использует файл?'" -ForegroundColor White
}
catch {
    Write-Host "`n[ERROR] $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

