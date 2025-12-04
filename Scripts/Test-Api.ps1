# Тестирование FileMonitorService API
# Проверяет доступность всех endpoints

param(
    [string]$BaseUrl = "http://localhost:5000"
)

Write-Host "=== Тестирование FileMonitorService API ===" -ForegroundColor Cyan
Write-Host "URL: $BaseUrl`n" -ForegroundColor White

# Test 1: Health Check
Write-Host "[1/5] Health Check..." -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "$BaseUrl/api/files/health" -Method Get
    Write-Host "  ✓ Сервис работает" -ForegroundColor Green
    Write-Host "  Статус: $($response.status)" -ForegroundColor Gray
}
catch {
    Write-Host "  ✗ Ошибка: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Test 2: Get Active Files
Write-Host "`n[2/5] Получение активных файлов..." -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "$BaseUrl/api/files/active" -Method Get
    Write-Host "  ✓ Активных файлов: $($response.count)" -ForegroundColor Green
    if ($response.count -gt 0) {
        $response.files | Select-Object -First 5 | Format-Table FilePath, UserCount
    }
}
catch {
    Write-Host "  ✗ Ошибка: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 3: Refresh Cache
Write-Host "`n[3/5] Обновление кэша..." -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "$BaseUrl/api/files/refresh" -Method Post
    Write-Host "  ✓ Кэш обновлен" -ForegroundColor Green
    Write-Host "  Файлов в кэше: $($response.filesCount)" -ForegroundColor Gray
}
catch {
    Write-Host "  ✗ Ошибка: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 4: Get File Users (тестовый файл)
Write-Host "`n[4/5] Получение пользователей файла..." -ForegroundColor Yellow
$testFile = "C:\Share\test.txt"
try {
    $encodedPath = [System.Web.HttpUtility]::UrlEncode($testFile)
    $response = Invoke-RestMethod -Uri "$BaseUrl/api/files/users?filePath=$encodedPath" -Method Get
    Write-Host "  ✓ Файл открыт пользователями: $($response.userCount)" -ForegroundColor Green
    $response.users | Format-Table UserName, ClientName, AccessMode
}
catch {
    if ($_.Exception.Response.StatusCode -eq 404) {
        Write-Host "  ℹ Файл не открыт (это нормально для теста)" -ForegroundColor Cyan
    }
    else {
        Write-Host "  ✗ Ошибка: $($_.Exception.Message)" -ForegroundColor Red
    }
}

# Test 5: Get User Files
Write-Host "`n[5/5] Получение файлов пользователя..." -ForegroundColor Yellow
$testUser = $env:USERNAME
try {
    $response = Invoke-RestMethod -Uri "$BaseUrl/api/files/user/$testUser" -Method Get
    Write-Host "  ✓ Файлов пользователя $testUser`: $($response.count)" -ForegroundColor Green
    if ($response.count -gt 0) {
        $response.files | Format-Table FilePath, AccessMode
    }
}
catch {
    Write-Host "  ✗ Ошибка: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n=== Тестирование завершено ===" -ForegroundColor Cyan
