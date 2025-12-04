# Diagnose-Server.ps1
# Скрипт диагностики FileMonitorService на сервере
# Запускать на файловом сервере Windows Server 2019

Write-Host "========================================" -ForegroundColor Cyan
Write-Host " FileMonitorService - Диагностика" -ForegroundColor Cyan  
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# 1. Проверка службы
Write-Host "[1] Проверка службы FileMonitorService..." -ForegroundColor Yellow
$service = Get-Service -Name "FileMonitorService" -ErrorAction SilentlyContinue
if ($service) {
    if ($service.Status -eq "Running") {
        Write-Host "    ✓ Служба запущена" -ForegroundColor Green
    } else {
        Write-Host "    ✗ Служба остановлена (Status: $($service.Status))" -ForegroundColor Red
        Write-Host "    → Запуск: Start-Service FileMonitorService" -ForegroundColor Gray
    }
} else {
    Write-Host "    ✗ Служба не установлена!" -ForegroundColor Red
    Write-Host "    → Переустановите MSI с выбором 'Серверная служба'" -ForegroundColor Gray
}
Write-Host ""

# 2. Проверка порта
Write-Host "[2] Проверка прослушивания порта 5000..." -ForegroundColor Yellow
$listening = netstat -an | Select-String ":5000.*LISTENING"
if ($listening) {
    Write-Host "    ✓ Порт 5000 слушается" -ForegroundColor Green
    $listening | ForEach-Object { Write-Host "      $_" -ForegroundColor Gray }
} else {
    Write-Host "    ✗ Порт 5000 не слушается!" -ForegroundColor Red
    Write-Host "    → Проверьте логи службы в папке установки\logs" -ForegroundColor Gray
}
Write-Host ""

# 3. Проверка API локально
Write-Host "[3] Проверка HTTP API локально..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "http://localhost:5000/api/files/health" -UseBasicParsing -TimeoutSec 5
    if ($response.StatusCode -eq 200) {
        Write-Host "    ✓ API отвечает (HTTP 200 OK)" -ForegroundColor Green
        Write-Host "      Ответ: $($response.Content)" -ForegroundColor Gray
    }
} catch {
    Write-Host "    ✗ API не отвечает: $($_.Exception.Message)" -ForegroundColor Red
}
Write-Host ""

# 4. Проверка файрвола
Write-Host "[4] Проверка правила файрвола..." -ForegroundColor Yellow
$rule = Get-NetFirewallRule -DisplayName "*FileMonitor*" -ErrorAction SilentlyContinue
if ($rule) {
    Write-Host "    ✓ Правило файрвола найдено" -ForegroundColor Green
    $rule | ForEach-Object { Write-Host "      $($_.DisplayName): $($_.Enabled)" -ForegroundColor Gray }
} else {
    Write-Host "    ✗ Правило файрвола не найдено!" -ForegroundColor Red
    Write-Host "    → Создание правила:" -ForegroundColor Gray
    Write-Host '      New-NetFirewallRule -DisplayName "FileMonitor Service" -Direction Inbound -Protocol TCP -LocalPort 5000 -Action Allow' -ForegroundColor Cyan
}
Write-Host ""

# 5. Проверка IP адресов
Write-Host "[5] IP адреса этого сервера:" -ForegroundColor Yellow
Get-NetIPAddress -AddressFamily IPv4 | Where-Object { $_.IPAddress -ne "127.0.0.1" } | ForEach-Object {
    Write-Host "    • $($_.IPAddress) ($($_.InterfaceAlias))" -ForegroundColor Cyan
}
Write-Host ""

# 6. Проверка Get-SmbOpenFile
Write-Host "[6] Проверка Get-SmbOpenFile (требует прав администратора)..." -ForegroundColor Yellow
try {
    $files = Get-SmbOpenFile -ErrorAction Stop | Select-Object -First 5
    if ($files) {
        Write-Host "    ✓ Get-SmbOpenFile работает (найдено файлов: $($files.Count))" -ForegroundColor Green
    } else {
        Write-Host "    ✓ Get-SmbOpenFile работает (открытых файлов нет)" -ForegroundColor Green
    }
} catch {
    Write-Host "    ✗ Ошибка: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "    → Запустите скрипт от имени администратора" -ForegroundColor Gray
}
Write-Host ""

# 7. Итог
Write-Host "========================================" -ForegroundColor Cyan
Write-Host " Адрес для клиентов:" -ForegroundColor Cyan
$mainIP = (Get-NetIPAddress -AddressFamily IPv4 | Where-Object { $_.IPAddress -ne "127.0.0.1" -and $_.PrefixOrigin -ne "WellKnown" } | Select-Object -First 1).IPAddress
if ($mainIP) {
    Write-Host "   http://${mainIP}:5000" -ForegroundColor Green
    Write-Host "   или: http://$($env:COMPUTERNAME):5000" -ForegroundColor Green
} else {
    Write-Host "   http://$($env:COMPUTERNAME):5000" -ForegroundColor Green
}
Write-Host "========================================" -ForegroundColor Cyan
