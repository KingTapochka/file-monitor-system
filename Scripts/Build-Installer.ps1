# Skript sborki MSI ustanovschika dlya File Monitor System
# Zapuskat iz kornya proekta

param(
    [switch]$Clean,
    [switch]$SkipTests
)

$ErrorActionPreference = "Stop"

Write-Host "=== Sborka File Monitor System MSI Installer ===" -ForegroundColor Cyan

$rootPath = $PSScriptRoot | Split-Path -Parent

# Proverka WiX Toolset
Write-Host "`n[1/6] Proverka WiX Toolset..." -ForegroundColor Yellow
$wixPath = "$env:USERPROFILE\.dotnet\tools\wix.exe"
if (-not (Test-Path $wixPath)) {
    Write-Error "WiX Toolset ne nayden! Ustanovite: dotnet tool install --global wix --version 4.0.4"
    Write-Host "Posle ustanovki perezapustite PowerShell" -ForegroundColor Yellow
    exit 1
}
try {
    $wixVersion = & $wixPath --version 2>&1
    Write-Host "  [OK] WiX ustanovlen: $wixVersion" -ForegroundColor Green
}
catch {
    Write-Error "Oshibka pri zapuske WiX: $_"
    exit 1
}

# Ochistka predydushchih sborok
if ($Clean) {
    Write-Host "`n[2/6] Ochistka predydushchih sborok..." -ForegroundColor Yellow
    
    Remove-Item "$rootPath\FileMonitorService\bin" -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item "$rootPath\FileMonitorService\obj" -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item "$rootPath\FileMonitorClient\bin" -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item "$rootPath\FileMonitorClient\obj" -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item "$rootPath\Installer\bin" -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item "$rootPath\Installer\obj" -Recurse -Force -ErrorAction SilentlyContinue
    
    Write-Host "  [OK] Ochistka zavershena" -ForegroundColor Green
}
else {
    Write-Host "`n[2/6] Propusk ochistki (ispolzuyte -Clean dlya ochistki)" -ForegroundColor Yellow
}

# Sborka servernoy chasti
Write-Host "`n[3/6] Sborka FileMonitorService..." -ForegroundColor Yellow
Push-Location "$rootPath\FileMonitorService"
try {
    dotnet restore
    dotnet publish -c Release --self-contained false -o "bin\Release\net8.0-windows\publish"
    
    if ($LASTEXITCODE -ne 0) {
        throw "Oshibka pri sborke FileMonitorService"
    }
    
    Write-Host "  [OK] FileMonitorService sobran" -ForegroundColor Green
}
finally {
    Pop-Location
}

# Sborka klientskoy chasti
Write-Host "`n[4/6] Sborka FileMonitorClient..." -ForegroundColor Yellow
Push-Location "$rootPath\FileMonitorClient"
try {
    dotnet restore
    dotnet build -c Release
    
    if ($LASTEXITCODE -ne 0) {
        throw "Oshibka pri sborke FileMonitorClient"
    }
    
    Write-Host "  [OK] FileMonitorClient sobran" -ForegroundColor Green
}
finally {
    Pop-Location
}

# Sozdanie konfiguracionnogo fayla dlya klienta (esli ne suschestvuet)
$clientConfigPath = "$rootPath\FileMonitorClient\bin\Release\net48\FileMonitorClient.dll.config"
if (-not (Test-Path $clientConfigPath)) {
    Write-Host "`n  Sozdanie konfiguracionnogo fayla klienta..." -ForegroundColor Cyan
    @"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <appSettings>
    <add key="ApiBaseUrl" value="http://localhost:5000" />
  </appSettings>
</configuration>
"@ | Out-File -FilePath $clientConfigPath -Encoding UTF8
}

# Sborka ustanovschika
Write-Host "`n[5/6] Sborka MSI ustanovschika..." -ForegroundColor Yellow
Push-Location "$rootPath\Installer"
try {
    # Sborka cherez wix CLI s bazovym UI
    Write-Host "  Ispolzovanie wix CLI..." -ForegroundColor Cyan
    
    & $wixPath build Product.wxs ServerComponent.wxs ClientComponent.wxs `
        -arch x64 `
        -out "FileMonitorSetup.msi"
        
    if ($LASTEXITCODE -ne 0) {
        throw "Oshibka pri sborke ustanovschika"
    }
    
    Write-Host "  [OK] MSI ustanovschik sobran" -ForegroundColor Green
}
finally {
    Pop-Location
}

# Poisk rezultata
Write-Host "`n[6/6] Poisk rezultata..." -ForegroundColor Yellow

$msiPaths = @(
    "$rootPath\Installer\bin\Release\FileMonitorSetup.msi",
    "$rootPath\Installer\FileMonitorSetup.msi"
)

$msiPath = $null
foreach ($path in $msiPaths) {
    if (Test-Path $path) {
        $msiPath = $path
        break
    }
}

if ($msiPath) {
    $msiInfo = Get-Item $msiPath
    Write-Host "`n[OK] Sborka zavershena uspeshno!" -ForegroundColor Green
    Write-Host "`nRezultat:" -ForegroundColor Cyan
    Write-Host "  Fayl: $($msiInfo.FullName)" -ForegroundColor White
    Write-Host "  Razmer: $([math]::Round($msiInfo.Length / 1MB, 2)) MB" -ForegroundColor White
    Write-Host "  Data: $($msiInfo.LastWriteTime)" -ForegroundColor White
    
    Write-Host "`nDlya ustanovki vypolnite:" -ForegroundColor Yellow
    Write-Host "  msiexec /i `"$($msiInfo.FullName)`"" -ForegroundColor Gray
    
    Write-Host "`nDlya ustanovki tolko servera:" -ForegroundColor Yellow
    Write-Host "  msiexec /i `"$($msiInfo.FullName)`" ADDLOCAL=ServerFeature /qb" -ForegroundColor Gray
    
    Write-Host "`nDlya ustanovki tolko klienta:" -ForegroundColor Yellow
    Write-Host "  msiexec /i `"$($msiInfo.FullName)`" ADDLOCAL=ClientFeature /qb" -ForegroundColor Gray
}
else {
    Write-Error "MSI fayl ne nayden posle sborki!"
    exit 1
}

Write-Host "`n=== Sborka zavershena ===" -ForegroundColor Cyan
