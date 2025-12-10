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
    Write-Host "`n[2/5] Ochistka predydushchih sborok..." -ForegroundColor Yellow
    
    Remove-Item "$rootPath\FileMonitorService\bin" -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item "$rootPath\FileMonitorService\obj" -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item "$rootPath\FileMonitorApp\bin" -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item "$rootPath\FileMonitorApp\obj" -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item "$rootPath\Installer\bin" -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item "$rootPath\Installer\obj" -Recurse -Force -ErrorAction SilentlyContinue
    
    Write-Host "  [OK] Ochistka zavershena" -ForegroundColor Green
}
else {
    Write-Host "`n[2/5] Propusk ochistki (ispolzuyte -Clean dlya ochistki)" -ForegroundColor Yellow
}

# Sborka servernoy chasti (single-file self-contained)
Write-Host "`n[3/5] Sborka FileMonitorService..." -ForegroundColor Yellow
Push-Location "$rootPath\FileMonitorService"
try {
    dotnet restore
    dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true -o "bin\Release\publish"
    
    if ($LASTEXITCODE -ne 0) {
        throw "Oshibka pri sborke FileMonitorService"
    }
    
    # Garantiruem nalichie handle64.exe dlya servernogo komponenta
    $handlePath = "bin\Release\net8.0-windows\win-x64\handle64.exe"
    if (-not (Test-Path $handlePath)) {
        Write-Host "  Zagruzka handle64.exe (Sysinternals)..." -ForegroundColor Gray
        $tempZip = Join-Path $env:TEMP "Handle.zip"
        $tempDir = Join-Path $env:TEMP "handle"
        Remove-Item $tempZip -Force -ErrorAction SilentlyContinue
        Remove-Item $tempDir -Recurse -Force -ErrorAction SilentlyContinue
        Invoke-WebRequest -Uri "https://download.sysinternals.com/files/Handle.zip" -OutFile $tempZip -UseBasicParsing
        Expand-Archive -Path $tempZip -DestinationPath $tempDir -Force
        $downloaded = Join-Path $tempDir "handle64.exe"
        if (-not (Test-Path $downloaded)) {
            $downloaded = Join-Path $tempDir "handle.exe"
        }
        Copy-Item $downloaded -Destination $handlePath -Force
        Remove-Item $tempZip -Force -ErrorAction SilentlyContinue
        Remove-Item $tempDir -Recurse -Force -ErrorAction SilentlyContinue
    }

    Write-Host "  [OK] FileMonitorService sobran" -ForegroundColor Green
}
finally {
    Pop-Location
}

# Sborka klientskoy chasti (FileMonitorApp)
Write-Host "`n[4/5] Sborka FileMonitorApp..." -ForegroundColor Yellow
Push-Location "$rootPath\FileMonitorApp"
try {
    dotnet restore
    dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true -o "bin\Release\publish"
    
    if ($LASTEXITCODE -ne 0) {
        throw "Oshibka pri sborke FileMonitorApp"
    }

    $appConfigSource = Join-Path $PWD "App.config"
    $appConfigTarget = "bin\Release\publish\FileMonitorApp.dll.config"
    if (Test-Path $appConfigSource) {
        Copy-Item $appConfigSource $appConfigTarget -Force
    }
    
    Write-Host "  [OK] FileMonitorApp sobran" -ForegroundColor Green
}
finally {
    Pop-Location
}

# Sborka ustanovschika
Write-Host "`n[5/5] Sborka MSI ustanovschika..." -ForegroundColor Yellow
Push-Location "$rootPath\Installer"
try {
    # Sborka cherez wix CLI s bazovym UI
    Write-Host "  Ispolzovanie wix CLI..." -ForegroundColor Cyan
    
    & $wixPath build Product.wxs ServerComponent.wxs ClientComponent.wxs `
        -arch x64 `
        -ext WixToolset.UI.wixext `
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
Write-Host "`n Poisk rezultata..." -ForegroundColor Yellow

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
