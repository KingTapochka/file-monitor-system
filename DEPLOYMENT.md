# Руководство по развертыванию File Monitor System

## Системные требования

### Серверная часть (FileMonitorService)
- **ОС:** Windows Server 2016+ или Windows 10+
- **Runtime:** .NET 8.0 Runtime
- **PowerShell:** 5.1 или выше
- **Роль:** File Server с SMB
- **Права:** Администратор (для Get-SmbOpenFile)
- **Порты:** 5000 (HTTP), 5001 (HTTPS)

### Клиентская часть (FileMonitorClient)
- **ОС:** Windows 10 22H2 или Windows 11
- **Framework:** .NET Framework 4.8
- **Права:** Администратор (только для установки)

---

## Развертывание серверной части

### Шаг 1: Установка .NET 8.0 Runtime

```powershell
# Загрузите и установите .NET 8.0 Runtime с microsoft.com
# Или используйте winget:
winget install Microsoft.DotNet.Runtime.8
```

### Шаг 2: Клонирование и сборка проекта

```powershell
# Клонируйте репозиторий
git clone <repository-url>
cd monitoring_polzovateley

# Сборка проекта
cd FileMonitorService
dotnet restore
dotnet build -c Release
```

### Шаг 3: Установка службы

```powershell
# Запустите скрипт установки от имени администратора
cd ..\Scripts
.\Install-Service.ps1

# Или вручную:
cd ..\FileMonitorService
dotnet publish -c Release -o C:\Services\FileMonitorService
sc create FileMonitorService binPath="C:\Services\FileMonitorService\FileMonitorService.exe"
sc start FileMonitorService
```

### Шаг 4: Проверка работоспособности

```powershell
# Проверка службы
Get-Service FileMonitorService

# Проверка API
.\Scripts\Test-Api.ps1

# Или вручную:
Invoke-RestMethod -Uri "http://localhost:5000/api/files/health"
```

### Шаг 5: Настройка Firewall (если клиенты на других машинах)

```powershell
# Разрешить входящие подключения
New-NetFirewallRule -DisplayName "FileMonitorService HTTP" `
    -Direction Inbound -Protocol TCP -LocalPort 5000 -Action Allow

New-NetFirewallRule -DisplayName "FileMonitorService HTTPS" `
    -Direction Inbound -Protocol TCP -LocalPort 5001 -Action Allow
```

### Шаг 6: Настройка HTTPS (рекомендуется для продакшена)

```powershell
# Создание самоподписанного сертификата (для тестирования)
$cert = New-SelfSignedCertificate -DnsName "fileserver.domain.local" `
    -CertStoreLocation "cert:\LocalMachine\My" `
    -KeyUsage DigitalSignature, KeyEncipherment

# Привязка сертификата к порту 5001
netsh http add sslcert ipport=0.0.0.0:5001 `
    certhash=$($cert.Thumbprint) appid='{12345678-1234-1234-1234-123456789012}'

# Обновите appsettings.json с настройками сертификата
```

---

## Развертывание клиентской части

### Шаг 1: Установка .NET Framework 4.8

```powershell
# Проверка версии
Get-ItemProperty "HKLM:\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full" | 
    Select-Object -ExpandProperty Release

# Если меньше 528040, установите .NET Framework 4.8
```

### Шаг 2: Настройка адреса сервера

Отредактируйте `FileMonitorClient\Services\FileMonitorApiClient.cs`:

```csharp
public FileMonitorApiClient(string baseUrl = "http://your-file-server:5000")
```

### Шаг 3: Сборка проекта

```powershell
cd FileMonitorClient
dotnet restore
dotnet build -c Release
```

### Шаг 4: Установка Shell Extension

```powershell
# Запустите от имени администратора
cd ..\Scripts
.\Install-Client.ps1 -Force

# Или вручную:
cd ..\FileMonitorClient\bin\Release\net48
regasm FileMonitorClient.dll /codebase

# Перезапуск Explorer
taskkill /f /im explorer.exe
explorer.exe
```

### Шаг 5: Проверка установки

1. Откройте Windows Explorer
2. Нажмите правой кнопкой на любом файле
3. Должен появиться пункт "Кто использует файл?"

---

## Тестирование системы

### Сценарий тестирования

1. **На Server:**
   ```powershell
   # Создайте тестовую папку
   New-Item -ItemType Directory -Path "C:\TestShare"
   New-SmbShare -Name "TestShare" -Path "C:\TestShare" -FullAccess "Everyone"
   
   # Создайте тестовый файл
   "Test content" | Out-File "C:\TestShare\test.txt"
   ```

2. **На Client1:**
   ```powershell
   # Откройте файл
   notepad.exe \\server\TestShare\test.txt
   ```

3. **На Client2:**
   ```powershell
   # Проверьте через Shell Extension
   # 1. Откройте \\server\TestShare в Explorer
   # 2. ПКМ на test.txt → "Кто использует файл?"
   # 3. Должен отобразиться Client1 в списке
   ```

4. **Проверка через API:**
   ```powershell
   $filePath = "C:\TestShare\test.txt"
   $encodedPath = [System.Web.HttpUtility]::UrlEncode($filePath)
   Invoke-RestMethod -Uri "http://server:5000/api/files/users?filePath=$encodedPath"
   ```

---

## Массовое развертывание

### Через Group Policy (GPO)

1. **Создайте пакет MSI** (или используйте скрипты PowerShell)

2. **Создайте новый GPO:**
   - Computer Configuration → Policies → Software Settings → Software Installation
   - Добавьте пакет установки FileMonitorClient

3. **Настройте startup script:**
   ```powershell
   # В GPO: Computer Configuration → Policies → Windows Settings → Scripts (Startup)
   # Добавьте Install-Client.ps1
   ```

### Через SCCM/Configuration Manager

```powershell
# Создайте Package с Install-Client.ps1
# Создайте Deployment для целевых компьютеров
```

### Через PowerShell Remoting

```powershell
$computers = Get-ADComputer -Filter * | Select-Object -ExpandProperty Name

foreach ($computer in $computers) {
    Invoke-Command -ComputerName $computer -ScriptBlock {
        # Копирование файлов
        Copy-Item "\\server\Deploy\FileMonitorClient\*" -Destination "C:\Temp\FileMonitorClient" -Recurse
        
        # Установка
        cd C:\Temp\FileMonitorClient
        .\Install-Client.ps1 -Force
    }
}
```

---

## Обслуживание

### Просмотр логов службы

```powershell
# Логи FileMonitorService
Get-Content "C:\Services\FileMonitorService\logs\service-*.txt" -Tail 50 -Wait

# Event Viewer
Get-EventLog -LogName Application -Source FileMonitorService -Newest 10
```

### Обновление службы

```powershell
# Остановка службы
Stop-Service FileMonitorService

# Замена файлов
Copy-Item ".\new-version\*" "C:\Services\FileMonitorService\" -Force

# Запуск службы
Start-Service FileMonitorService
```

### Обновление клиента

```powershell
# На каждом клиенте:
.\Uninstall-Client.ps1 -Force
.\Install-Client.ps1 -Force
```

---

## Устранение неполадок

### Служба не запускается

```powershell
# Проверка прав
whoami /groups | findstr "S-1-5-32-544"  # Администраторы

# Проверка зависимостей
Get-Service | Where-Object {$_.Name -like "*FileMonitor*"}

# Ручной запуск для отладки
cd C:\Services\FileMonitorService
.\FileMonitorService.exe

# Логи
Get-Content logs\service-*.txt -Tail 100
```

### Shell Extension не работает

```powershell
# Проверка регистрации
reg query "HKCR\*\shellex\ContextMenuHandlers" /s | findstr "FileMonitor"

# Переустановка
.\Uninstall-Client.ps1 -Force
.\Install-Client.ps1 -Force

# Полная перезагрузка
Restart-Computer
```

### API недоступен

```powershell
# Проверка службы
Get-Service FileMonitorService

# Проверка портов
netstat -ano | findstr "5000"

# Проверка firewall
Get-NetFirewallRule | Where-Object {$_.DisplayName -like "*FileMonitor*"}

# Тест локально на сервере
Invoke-RestMethod -Uri "http://localhost:5000/api/files/health"
```

### PowerShell ошибки Get-SmbOpenFile

```powershell
# Проверка наличия команды
Get-Command Get-SmbOpenFile

# Проверка прав
Get-SmbOpenFile  # Должна вернуть данные или пустой список

# Если команды нет, установите:
Install-WindowsFeature FS-FileServer -IncludeManagementTools
```

---

## Безопасность

### Рекомендации

1. **Используйте HTTPS** в продакшене
2. **Включите Windows Authentication** для API
3. **Ограничьте доступ** к портам через Firewall
4. **Регулярно обновляйте** зависимости
5. **Мониторьте логи** на подозрительную активность

### Настройка Windows Authentication

В `Program.cs` добавьте:

```csharp
builder.Services.AddAuthentication(NegotiateDefaults.AuthenticationScheme)
    .AddNegotiate();

app.UseAuthentication();
app.UseAuthorization();
```

В `FilesController.cs`:

```csharp
[Authorize]
[ApiController]
public class FilesController : ControllerBase
```

---

## Производительность

### Оптимизация для больших серверов

В `appsettings.json`:

```json
{
  "FileMonitor": {
    "RefreshIntervalSeconds": 30,  // Увеличить интервал
    "CacheExpirationMinutes": 10    // Увеличить время кэша
  }
}
```

### Мониторинг производительности

```powershell
# CPU и память службы
Get-Process -Name FileMonitorService | 
    Select-Object CPU, WorkingSet, Handles

# Счетчики производительности
Get-Counter '\Process(FileMonitorService)\% Processor Time'
```

---

## Контакты и поддержка

При возникновении проблем:
1. Проверьте логи службы
2. Проверьте Event Viewer
3. Запустите Test-Api.ps1
4. Соберите информацию о системе

Сбор информации для отчета:

```powershell
$info = @{
    OS = (Get-WmiObject Win32_OperatingSystem).Caption
    PowerShell = $PSVersionTable.PSVersion
    DotNet = (Get-ItemProperty "HKLM:\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full").Version
    Service = (Get-Service FileMonitorService).Status
}
$info | ConvertTo-Json
```
