#Requires -RunAsAdministrator

<#
.SYNOPSIS
    Отмена регистрации Shell Extension для FileMonitorClient
#>

param(
    [Parameter(Mandatory=$false)]
    [string]$ClientPath = "C:\Program Files\File Monitor System\Client"
)

Write-Host "=== Отмена регистрации FileMonitor Shell Extension ===" -ForegroundColor Cyan

try {
    $dllPath = Join-Path $ClientPath "FileMonitorClient.dll"
    
    Write-Host "[1/4] Остановка Explorer..." -ForegroundColor Yellow
    Get-Process explorer -ErrorAction SilentlyContinue | Stop-Process -Force
    Start-Sleep -Seconds 2

    Write-Host "[2/4] Отмена регистрации (32-bit)..." -ForegroundColor Yellow
    $regasm32 = "$env:SystemRoot\Microsoft.NET\Framework\v4.0.30319\RegAsm.exe"
    if (Test-Path $regasm32) {
        & $regasm32 "$dllPath" /unregister /nologo
        Write-Host "  [OK] 32-bit отменена" -ForegroundColor Green
    }

    Write-Host "[3/4] Отмена регистрации (64-bit)..." -ForegroundColor Yellow
    $regasm64 = "$env:SystemRoot\Microsoft.NET\Framework64\v4.0.30319\RegAsm.exe"
    if (Test-Path $regasm64) {
        & $regasm64 "$dllPath" /unregister /nologo
        Write-Host "  [OK] 64-bit отменена" -ForegroundColor Green
    }

    Write-Host "[4/4] Запуск Explorer..." -ForegroundColor Yellow
    Start-Process explorer.exe
    Start-Sleep -Seconds 2

    Write-Host "`n[SUCCESS] Shell Extension удалено!" -ForegroundColor Green
}
catch {
    Write-Host "`n[ERROR] $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

