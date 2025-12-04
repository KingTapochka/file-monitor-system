# File Monitor System - Udalenie

param(
    [ValidateSet("Server", "Client", "Both")]
    [string]$Component = "Both"
)

$ErrorActionPreference = "Stop"

Write-Host "`n=== File Monitor System - Udalenie ===`n" -ForegroundColor Cyan

# Proverka prav administratora
$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Error "Trebuetsya prava administratora!"
    Read-Host "Nazhmite Enter"
    exit 1
}

# Menu vybora
if ($Component -eq "Both") {
    Write-Host "Chto udalit?:" -ForegroundColor Yellow
    Write-Host "  1 - Servernuyu sluzhbu"
    Write-Host "  2 - Klientskoe rasshirenie"
    Write-Host "  3 - Vse komponenty"
    Write-Host "  4 - Otmena"
    
    $choice = Read-Host "`nVash vybor (1-4)"
    
    switch ($choice) {
        "1" { $Component = "Server" }
        "2" { $Component = "Client" }
        "3" { $Component = "Both" }
        default { Write-Host "Otmeneno"; exit 0 }
    }
}

Write-Host "`n[INFO] Nachinaem udalenie: $Component`n" -ForegroundColor Cyan

# SERVER
if ($Component -in @("Server", "Both")) {
    Write-Host "[SERVER] Udalenie servernogo komponenta..." -ForegroundColor Yellow
    
    # Udalenie sluzhby
    $existingService = Get-Service -Name FileMonitorService -ErrorAction SilentlyContinue
    if ($existingService) {
        Write-Host "  Ostanovka sluzhby..." -ForegroundColor Cyan
        Stop-Service -Name FileMonitorService -Force -ErrorAction SilentlyContinue
        Start-Sleep -Seconds 2
        
        Write-Host "  Udalenie sluzhby..." -ForegroundColor Cyan
        sc.exe delete FileMonitorService | Out-Null
        Start-Sleep -Seconds 2
        
        Write-Host "  [OK] Sluzhba udalena!" -ForegroundColor Green
    }
    else {
        Write-Host "  [INFO] Sluzhba ne ustanovlena" -ForegroundColor Gray
    }
    
    # Udalenie faylov
    $serverPath = "C:\Program Files\File Monitor System\Service"
    if (Test-Path $serverPath) {
        Write-Host "  Udalenie faylov..." -ForegroundColor Cyan
        Remove-Item -Path $serverPath -Recurse -Force -ErrorAction SilentlyContinue
        Write-Host "  [OK] Fayli udaleny!" -ForegroundColor Green
    }
}

# CLIENT
if ($Component -in @("Client", "Both")) {
    Write-Host "`n[CLIENT] Udalenie klientskogo komponenta..." -ForegroundColor Yellow
    
    $clientPath = "C:\Program Files\File Monitor System\Client"
    
    if (Test-Path "$clientPath\FileMonitorClient.dll") {
        # Otregistraciya COM
        Write-Host "  Otregistraciya Shell Extension..." -ForegroundColor Cyan
        
        # Perestart Explorer
        taskkill /f /im explorer.exe 2>&1 | Out-Null
        Start-Sleep -Seconds 2
        
        # Otregistraciya
        $regasm32 = "$env:WINDIR\Microsoft.NET\Framework\v4.0.30319\RegAsm.exe"
        if (Test-Path $regasm32) {
            & $regasm32 "$clientPath\FileMonitorClient.dll" /unregister /nologo 2>&1 | Out-Null
        }
        
        $regasm64 = "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\RegAsm.exe"
        if (Test-Path $regasm64) {
            & $regasm64 "$clientPath\FileMonitorClient.dll" /unregister /nologo 2>&1 | Out-Null
        }
        
        # Zapusk Explorer
        Start-Process explorer.exe
        Start-Sleep -Seconds 2
        
        Write-Host "  [OK] Shell Extension otregistrirovano!" -ForegroundColor Green
    }
    else {
        Write-Host "  [INFO] Klient ne ustanovlen" -ForegroundColor Gray
    }
    
    # Udalenie faylov
    if (Test-Path $clientPath) {
        Write-Host "  Udalenie faylov..." -ForegroundColor Cyan
        Remove-Item -Path $clientPath -Recurse -Force -ErrorAction SilentlyContinue
        Write-Host "  [OK] Fayli udaleny!" -ForegroundColor Green
    }
}

# Udalenie osnovnoy papki esli pusta
$basePath = "C:\Program Files\File Monitor System"
if (Test-Path $basePath) {
    $items = Get-ChildItem $basePath -ErrorAction SilentlyContinue
    if ($items.Count -eq 0) {
        Remove-Item -Path $basePath -Force -ErrorAction SilentlyContinue
        Write-Host "`n[OK] Osnovnaya papka udalena" -ForegroundColor Green
    }
}

Write-Host "`n=== Udalenie zaversheno! ===`n" -ForegroundColor Green

Read-Host "Nazhmite Enter dlya zaversheniya"
