#Requires -RunAsAdministrator
<#
.SYNOPSIS
    Добавляет пункт "Проверка файла" в контекстное меню Windows
.DESCRIPTION
    Скрипт добавляет пункт контекстного меню для всех типов файлов,
    который запускает File Monitor для проверки использования файла
.EXAMPLE
    .\Add-ContextMenu.ps1
    Добавляет пункт меню (требует права администратора)
.EXAMPLE
    .\Add-ContextMenu.ps1 -Remove
    Удаляет пункт меню
#>

param(
    [switch]$Remove
)

$ErrorActionPreference = "Stop"

# Путь к exe файлу
$appPath = "C:\Program Files\FileMonitor\Client\FileMonitorApp.exe"

# Ключи реестра для контекстного меню
$menuName = "FileMonitor.CheckFile"
$menuText = "🔍 Проверка файла"

# Ключ для всех файлов
$regPath = "HKLM:\SOFTWARE\Classes\*\shell\$menuName"

function Add-ContextMenu {
    Write-Host "Добавление пункта контекстного меню..." -ForegroundColor Cyan
    
    # Проверяем наличие exe
    if (-not (Test-Path $appPath)) {
        Write-Warning "Файл $appPath не найден!"
        Write-Host "Укажите путь к FileMonitorApp.exe:" -ForegroundColor Yellow
        $customPath = Read-Host
        if (Test-Path $customPath) {
            $script:appPath = $customPath
        } else {
            throw "Файл не найден: $customPath"
        }
    }
    
    # Создаем ключ меню
    if (-not (Test-Path $regPath)) {
        New-Item -Path $regPath -Force | Out-Null
    }
    
    # Устанавливаем текст меню
    Set-ItemProperty -Path $regPath -Name "(Default)" -Value $menuText
    Set-ItemProperty -Path $regPath -Name "Icon" -Value "`"$appPath`",0"
    
    # Создаем ключ command
    $commandPath = "$regPath\command"
    if (-not (Test-Path $commandPath)) {
        New-Item -Path $commandPath -Force | Out-Null
    }
    
    # Устанавливаем команду
    Set-ItemProperty -Path $commandPath -Name "(Default)" -Value "`"$appPath`" `"%1`""
    
    Write-Host "✅ Пункт меню успешно добавлен!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Теперь при правом клике на любой файл появится пункт '$menuText'" -ForegroundColor Gray
}

function Remove-ContextMenu {
    Write-Host "Удаление пункта контекстного меню..." -ForegroundColor Cyan
    
    if (Test-Path $regPath) {
        Remove-Item -Path $regPath -Recurse -Force
        Write-Host "✅ Пункт меню успешно удален!" -ForegroundColor Green
    } else {
        Write-Host "Пункт меню не найден в реестре" -ForegroundColor Yellow
    }
}

# Основная логика
try {
    if ($Remove) {
        Remove-ContextMenu
    } else {
        Add-ContextMenu
    }
} catch {
    Write-Host "❌ Ошибка: $_" -ForegroundColor Red
    exit 1
}
