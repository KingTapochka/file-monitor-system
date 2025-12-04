# File Monitor System - Prostoy ustanovschik
# Zapusk: .\Scripts\Install-All.ps1

param(
    [ValidateSet("Server", "Client", "Both")]
    [string]$Component = "Both"
)

$ErrorActionPreference = "Stop"
$rootPath = Split-Path -Parent $PSScriptRoot

Write-Host "`n=== File Monitor System - Ustanovka ===`n" -ForegroundColor Cyan

# Proverka prav administratora
$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Error "Trebuetsya prava administratora!"
    Read-Host "Nazhmite Enter"
    exit 1
}

# Menu vybora
if ($Component -eq "Both") {
    Write-Host "Vybor komponenta:" -ForegroundColor Yellow
    Write-Host "  1 - Servernaya sluzhba (dlya faylovogo servera)"
    Write-Host "  2 - Klientskoe rasshirenie (dlya rabochih stanciy)"
    Write-Host "  3 - Oba komponenta"
    Write-Host "  4 - Otmena"
    
    $choice = Read-Host "`nVash vybor (1-4)"
    
    switch ($choice) {
        "1" { $Component = "Server" }
        "2" { $Component = "Client" }
        "3" { $Component = "Both" }
        default { Write-Host "Otmeneno"; exit 0 }
    }
}

Write-Host "`n[INFO] Nachinaem ustanovku: $Component`n" -ForegroundColor Cyan

# SERVER
if ($Component -in @("Server", "Both")) {
    Write-Host "[SERVER] Ustanovka servernogo komponenta..." -ForegroundColor Yellow
    
    $serverPath = "C:\Program Files\File Monitor System\Service"
    
    # Sborka
    Write-Host "  Sborka proekta..." -ForegroundColor Cyan
    Push-Location "$rootPath\FileMonitorService"
    dotnet publish -c Release --self-contained false -o "$serverPath"
    Pop-Location
    
    # Udalenie staroy sluzhby
    $existingService = Get-Service -Name FileMonitorService -ErrorAction SilentlyContinue
    if ($existingService) {
        Write-Host "  Ostanovka staroy sluzhby..." -ForegroundColor Yellow
        Stop-Service -Name FileMonitorService -Force -ErrorAction SilentlyContinue
        Start-Sleep -Seconds 2
        sc.exe delete FileMonitorService | Out-Null
        Start-Sleep -Seconds 2
    }
    
    # Sozdanie sluzhby
    Write-Host "  Registraciya Windows Service..." -ForegroundColor Cyan
    $serviceExe = Join-Path $serverPath "FileMonitorService.exe"
    sc.exe create FileMonitorService binPath= "`"$serviceExe`"" start= auto | Out-Null
    sc.exe description FileMonitorService "Sluzhba monitoringa otkrytyh faylov" | Out-Null
    
    # Zapusk
    Write-Host "  Zapusk sluzhby..." -ForegroundColor Cyan
    Start-Service -Name FileMonitorService
    Start-Sleep -Seconds 5
    
    # Proverka
    $service = Get-Service -Name FileMonitorService
    if ($service.Status -eq "Running") {
        Write-Host "  [OK] Sluzhba zapuschena!" -ForegroundColor Green
        
        try {
            $response = Invoke-RestMethod -Uri "http://localhost:5000/api/files/health" -TimeoutSec 10
            Write-Host "  [OK] API dostupno: http://localhost:5000" -ForegroundColor Green
        }
        catch {
            Write-Warning "  API poka ne dostupen (mozhet potrebovatsya vremya)"
        }
    }
    else {
        Write-Error "  [ERROR] Sluzhba ne zapustilas!"
    }
}

# CLIENT
if ($Component -in @("Client", "Both")) {
    Write-Host "`n[CLIENT] Ustanovka klientskogo komponenta..." -ForegroundColor Yellow
    
    $clientPath = "C:\Program Files\File Monitor System\Client"
    
    # Sborka
    Write-Host "  Sborka proekta..." -ForegroundColor Cyan
    Push-Location "$rootPath\FileMonitorClient"
    dotnet build -c Release
    
    # Kopirovanie
    if (-not (Test-Path $clientPath)) {
        New-Item -ItemType Directory -Path $clientPath -Force | Out-Null
    }
    Copy-Item "bin\Release\net48\*" -Destination $clientPath -Recurse -Force
    Pop-Location
    
    # Registraciya
    Write-Host "  Registraciya Shell Extension..." -ForegroundColor Cyan
    
    # Perestart Explorer
    taskkill /f /im explorer.exe 2>&1 | Out-Null
    Start-Sleep -Seconds 2
    
    # Registraciya COM
    $regasm32 = "$env:WINDIR\Microsoft.NET\Framework\v4.0.30319\RegAsm.exe"
    if (Test-Path $regasm32) {
        & $regasm32 "$clientPath\FileMonitorClient.dll" /codebase /nologo 2>&1 | Out-Null
    }
    
    $regasm64 = "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\RegAsm.exe"
    if (Test-Path $regasm64) {
        & $regasm64 "$clientPath\FileMonitorClient.dll" /codebase /nologo 2>&1 | Out-Null
    }
    
    # Zapusk Explorer
    Start-Process explorer.exe
    Start-Sleep -Seconds 2
    
    Write-Host "  [OK] Shell Extension zaregistrirovano!" -ForegroundColor Green
}

# Itogi
Write-Host "`n=== Ustanovka zavershena! ===`n" -ForegroundColor Green

if ($Component -in @("Server", "Both")) {
    Write-Host "Server:" -ForegroundColor Cyan
    Write-Host "  API: http://localhost:5000" -ForegroundColor White
    Write-Host "  Swagger: https://localhost:5001/swagger" -ForegroundColor White
}

if ($Component -in @("Client", "Both")) {
    Write-Host "`nClient:" -ForegroundColor Cyan
    Write-Host "  Ispolzovanie: PKM na fayle -> 'Kto ispolzuet fayl?'" -ForegroundColor White
}

Write-Host "`nDlya udaleniya: .\Scripts\Uninstall-All.ps1`n" -ForegroundColor Yellow

Read-Host "Nazhmite Enter dlya zaversheniya"
