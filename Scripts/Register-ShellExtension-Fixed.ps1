#Requires -RunAsAdministrator

Write-Host @"
╔════════════════════════════════════════════════════════════╗
║ ПРАВИЛЬНАЯ РЕГИСТРАЦИЯ SHELL EXTENSION                    ║
╚════════════════════════════════════════════════════════════╝
"@ -ForegroundColor Cyan

$clientPath = "C:\Program Files\File Monitor System\Client"
$dllPath = Join-Path $clientPath "FileMonitorClient.dll"

if (-not (Test-Path $dllPath)) {
    Write-Host "[ERROR] Client not installed!" -ForegroundColor Red
    exit 1
}

Write-Host "`n[!] Explorer will be restarted`n" -ForegroundColor Yellow
Start-Sleep -Seconds 2

# Stop Explorer
Write-Host "[1/3] Stopping Explorer..." -ForegroundColor Yellow
Get-Process explorer -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 3

# Register using regasm WITHOUT /codebase (to avoid strong name requirement)
Write-Host "[2/3] Registering Shell Extension..." -ForegroundColor Yellow

$regasm32 = "$env:SystemRoot\Microsoft.NET\Framework\v4.0.30319\RegAsm.exe"
$regasm64 = "$env:SystemRoot\Microsoft.NET\Framework64\v4.0.30319\RegAsm.exe"

if (Test-Path $regasm64) {
    Write-Host "  Registering (64-bit)..." -ForegroundColor Cyan
    & $regasm64 "$dllPath" /nologo /tlb
}

if (Test-Path $regasm32) {
    Write-Host "  Registering (32-bit)..." -ForegroundColor Cyan
    & $regasm32 "$dllPath" /nologo /tlb
}

# Start Explorer
Write-Host "[3/3] Starting Explorer..." -ForegroundColor Yellow
Start-Process explorer.exe
Start-Sleep -Seconds 4

Write-Host "`n[SUCCESS] Registration complete!" -ForegroundColor Green
Write-Host "`nTest it:" -ForegroundColor Cyan
Write-Host "  1. Open any folder" -ForegroundColor White
Write-Host "  2. Right-click on a file" -ForegroundColor White
Write-Host "  3. Look for 'Who is using file?' menu item" -ForegroundColor White
Write-Host "`nIf menu item still missing - REBOOT the computer" -ForegroundColor Yellow
