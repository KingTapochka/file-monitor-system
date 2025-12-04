# 🎯 MSI Установщик - Полное руководство

## ✅ Что реализовано

Создан **полнофункциональный MSI установщик** на базе WiX Toolset 4.0 с возможностями:

### 🔧 Функции установщика:

1. **Выбор компонентов при установке:**
   - ☑️ Серверная служба (FileMonitorService)
   - ☑️ Клиентское расширение (Shell Extension)
   - ☑️ Оба компонента одновременно

2. **Автоматическая установка сервера:**
   - Копирование файлов службы
   - Создание Windows Service через `sc create`
   - Автоматический запуск службы
   - Создание папки для логов
   - Регистрация в реестре

3. **Автоматическая установка клиента:**
   - Копирование Shell Extension DLL
   - Регистрация COM (32-bit и 64-bit)
   - Перезапуск Windows Explorer
   - Регистрация в реестре

4. **Автоматическое удаление:**
   - Остановка и удаление Windows Service
   - Отмена регистрации Shell Extension
   - Удаление всех файлов
   - Очистка реестра
   - Корректная обработка ошибок

5. **Дополнительные возможности:**
   - Поддержка обновлений (upgrade)
   - Тихая установка через командную строку
   - Подробное логирование
   - Откат при ошибках

---

## 🚀 Быстрый старт

### 1️⃣ Установка WiX Toolset

```powershell
# Установка через .NET CLI
dotnet tool install --global wix --version 4.0.4

# Проверка
wix --version
```

### 2️⃣ Сборка установщика

```powershell
# Из корня проекта
cd c:\monitoring_polzovateley

# Запустите скрипт сборки
.\Scripts\Build-Installer.ps1

# MSI будет создан в: Installer\bin\Release\FileMonitorSetup.msi
```

### 3️⃣ Установка на сервер

```powershell
# С GUI (выбор компонентов)
msiexec /i "Installer\bin\Release\FileMonitorSetup.msi"

# Выберите: "Серверная служба"

# ИЛИ тихая установка
msiexec /i "Installer\bin\Release\FileMonitorSetup.msi" ADDLOCAL=ServerFeature /qn
```

### 4️⃣ Установка на клиенты

```powershell
# С GUI
msiexec /i "Installer\bin\Release\FileMonitorSetup.msi"

# Выберите: "Клиентское расширение"

# ИЛИ тихая установка
msiexec /i "Installer\bin\Release\FileMonitorSetup.msi" ADDLOCAL=ClientFeature /qn
```

---

## 📋 Структура установщика

```
Installer/
├── FileMonitorInstaller.wixproj   # WiX проект
├── Product.wxs                     # Главный файл (UI, CustomActions)
├── ServerComponent.wxs             # Серверные компоненты и файлы
├── ClientComponent.wxs             # Клиентские компоненты и файлы
├── README.md                       # Подробная документация
├── QUICKSTART.md                   # Быстрое руководство
└── GUIDE.md                        # Это руководство
```

### Файлы WXS:

**Product.wxs** - Основной файл:
- Метаданные продукта (версия, GUID, производитель)
- Определение Features (серверный/клиентский компоненты)
- UI последовательность (WixUI_FeatureTree)
- Custom Actions для установки/удаления
- Install/Uninstall последовательности

**ServerComponent.wxs** - Серверные компоненты:
- Копирование FileMonitorService.exe и зависимостей
- Конфигурационные файлы (appsettings.json)
- Создание папки logs
- Регистрация в реестре

**ClientComponent.wxs** - Клиентские компоненты:
- Копирование FileMonitorClient.dll
- Зависимости (SharpShell.dll, Newtonsoft.Json.dll)
- Конфигурационный файл
- Регистрация в реестре

---

## 🎛️ Параметры установки

### msiexec параметры:

```powershell
# Установка с GUI
msiexec /i FileMonitorSetup.msi

# Тихая установка (без UI)
msiexec /i FileMonitorSetup.msi /qn

# С базовым UI (только прогресс)
msiexec /i FileMonitorSetup.msi /qb

# С подробным логом
msiexec /i FileMonitorSetup.msi /l*v install.log

# Удаление
msiexec /x FileMonitorSetup.msi /qn
```

### Параметры компонентов:

```powershell
# Только сервер
ADDLOCAL=ServerFeature

# Только клиент
ADDLOCAL=ClientFeature

# Оба (по умолчанию)
ADDLOCAL=ALL
# или просто не указывать
```

### Примеры:

```powershell
# Тихая установка сервера с логом
msiexec /i FileMonitorSetup.msi ADDLOCAL=ServerFeature /qn /l*v server.log

# Базовый UI для клиента
msiexec /i FileMonitorSetup.msi ADDLOCAL=ClientFeature /qb

# Оба компонента с логом
msiexec /i FileMonitorSetup.msi /qb /l*v full.log

# Удаление с логом
msiexec /x FileMonitorSetup.msi /qn /l*v uninstall.log
```

---

## 🌐 Массовое развертывание

### Вариант 1: Group Policy (GPO)

**Для серверов:**

1. Скопируйте MSI: `\\dc\netlogon\software\FileMonitorSetup.msi`
2. Откройте GPMC.msc
3. Создайте GPO: "File Monitor Server Installation"
4. Computer Configuration → Policies → Software Settings → Software Installation
5. New → Package → Выберите MSI
6. Deployment Method: Assigned
7. В properties → Deployment → Advanced → не указывайте UI level (для GUI выбора)
8. Или создайте transform (.mst) для автоматического выбора ServerFeature

**Для клиентов:**

Аналогично, но с другим GPO для клиентских компьютеров.

### Вариант 2: PowerShell DSC

```powershell
Configuration FileMonitorInstallation {
    param (
        [string]$Component = "Client"
    )
    
    Import-DscResource -ModuleName PSDesiredStateConfiguration
    
    Node $AllNodes.NodeName {
        Package FileMonitor {
            Name = "File Monitor System"
            Path = "\\server\share\FileMonitorSetup.msi"
            ProductId = "{12345678-1234-1234-1234-123456789012}"
            Arguments = "ADDLOCAL=$($Component)Feature /qn"
            Ensure = "Present"
        }
    }
}
```

### Вариант 3: PowerShell Remoting

```powershell
# Массовая установка на серверы
$servers = "FileServer01", "FileServer02", "FileServer03"
$msiPath = "\\dc\share\FileMonitorSetup.msi"

Invoke-Command -ComputerName $servers -ScriptBlock {
    param($msi)
    Start-Process msiexec -ArgumentList "/i `"$msi`" ADDLOCAL=ServerFeature /qn" -Wait
} -ArgumentList $msiPath

# Массовая установка на клиенты
$computers = Get-ADComputer -Filter {OperatingSystem -like "*Windows 10*"} | 
             Select-Object -ExpandProperty Name

Invoke-Command -ComputerName $computers -ScriptBlock {
    param($msi)
    Start-Process msiexec -ArgumentList "/i `"$msi`" ADDLOCAL=ClientFeature /qn" -Wait
} -ArgumentList $msiPath -ThrottleLimit 10
```

### Вариант 4: SCCM/ConfigMgr

1. **Создайте Application:**
   - Name: File Monitor Server
   - Deployment Type: Windows Installer (*.msi)
   
2. **Installation program:**
   ```
   msiexec /i FileMonitorSetup.msi ADDLOCAL=ServerFeature /qn
   ```

3. **Uninstall program:**
   ```
   msiexec /x {12345678-1234-1234-1234-123456789012} /qn
   ```

4. **Detection Method:**
   - Registry: `HKLM\SOFTWARE\FileMonitorSystem`
   - Value: ServerPath или ClientPath

5. **Deployment:**
   - Deploy to соответствующие Device Collections

---

## ✅ Проверка установки

### Автоматическая проверка:

```powershell
# Запустите тест
.\Scripts\Test-Installer.ps1 -Component Server

# Или для клиента
.\Scripts\Test-Installer.ps1 -Component Client

# Или оба
.\Scripts\Test-Installer.ps1 -Component Both
```

### Ручная проверка сервера:

```powershell
# 1. Служба
Get-Service FileMonitorService
# Должно быть: Status=Running, StartType=Automatic

# 2. Файлы
Test-Path "C:\Program Files\File Monitor System\Service\FileMonitorService.exe"
# Должно быть: True

# 3. API
Invoke-RestMethod -Uri "http://localhost:5000/api/files/health"
# Должно вернуть: {"status":"healthy",...}

# 4. Реестр
Get-ItemProperty "HKLM:\SOFTWARE\FileMonitorSystem" -Name ServerPath
# Должно показать путь установки
```

### Ручная проверка клиента:

```powershell
# 1. Файлы
Test-Path "C:\Program Files\File Monitor System\Client\FileMonitorClient.dll"
# Должно быть: True

# 2. COM регистрация
reg query "HKCR\*\shellex\ContextMenuHandlers" /s | findstr "FileMonitor"
# Должно найти запись

# 3. Визуально
# Откройте Explorer → ПКМ на файле → Должен быть пункт "Кто использует файл?"
```

---

## 🐛 Устранение неполадок

### Проблема: Ошибка при сборке MSI

**Симптомы:**
```
Error: Failed to build MSI
```

**Решение:**
```powershell
# 1. Проверьте WiX
wix --version

# 2. Переустановите WiX
dotnet tool uninstall --global wix
dotnet tool install --global wix --version 4.0.4

# 3. Очистите и пересоберите
.\Scripts\Build-Installer.ps1 -Clean

# 4. Проверьте, что проекты собраны
cd FileMonitorService
dotnet publish -c Release
cd ..\FileMonitorClient
dotnet build -c Release
```

### Проблема: Служба не устанавливается

**Симптомы:**
- MSI установлен успешно
- Но служба не создана

**Решение:**
```powershell
# 1. Проверьте лог установки
Get-Content "$env:TEMP\install.log" | Select-String "error"

# 2. Проверьте Custom Actions
Get-Content "$env:TEMP\install.log" | Select-String "InstallServerService"

# 3. Установите вручную
sc create FileMonitorService binPath="C:\Program Files\File Monitor System\Service\FileMonitorService.exe"

# 4. Проверьте права
whoami /groups | findstr "S-1-5-32-544"  # Администраторы
```

### Проблема: Shell Extension не работает

**Симптомы:**
- Клиент установлен
- Но пункта меню нет

**Решение:**
```powershell
# 1. Перезапустите Explorer
taskkill /f /im explorer.exe
explorer.exe

# 2. Проверьте регистрацию
reg query "HKCR\*\shellex\ContextMenuHandlers" /s

# 3. Зарегистрируйте вручную
cd "C:\Program Files\File Monitor System\Client"
regasm FileMonitorClient.dll /codebase

# 4. Перезагрузите компьютер
Restart-Computer
```

### Проблема: Ошибка при удалении

**Симптомы:**
```
Error 1603: Fatal error during installation
```

**Решение:**
```powershell
# 1. Остановите службу вручную
sc stop FileMonitorService

# 2. Удалите через msiexec
msiexec /x FileMonitorSetup.msi /qn /l*v uninstall.log

# 3. Если не помогло, удалите вручную
sc delete FileMonitorService
regasm "C:\Program Files\File Monitor System\Client\FileMonitorClient.dll" /unregister
Remove-Item "C:\Program Files\File Monitor System" -Recurse -Force

# 4. Очистите реестр
Remove-Item "HKLM:\SOFTWARE\FileMonitorSystem" -Force
```

---

## 📊 Коды возврата msiexec

| Код | Значение |
|-----|----------|
| 0 | Успех |
| 1602 | Отменено пользователем |
| 1603 | Фатальная ошибка |
| 1618 | Другая установка уже выполняется |
| 1619 | Не удается открыть MSI пакет |
| 1625 | Не разрешено системной политикой |
| 3010 | Требуется перезагрузка |

---

## 📝 Чек-лист развертывания

### Подготовка:

- [ ] WiX Toolset установлен
- [ ] Проекты собраны (Server + Client)
- [ ] MSI создан и протестирован
- [ ] Права администратора есть

### Сервер:

- [ ] .NET 8 Runtime установлен
- [ ] PowerShell 5.1+ доступен
- [ ] SMB File Server настроен
- [ ] Порты 5000/5001 свободны
- [ ] Firewall настроен (если нужно)

### Клиент:

- [ ] .NET Framework 4.8 установлен
- [ ] Права на регистрацию COM есть
- [ ] Адрес API сервера известен

### После установки:

- [ ] Служба работает (сервер)
- [ ] API доступен (сервер)
- [ ] Shell Extension появилась (клиент)
- [ ] Тестирование выполнено
- [ ] Документация передана пользователям

---

## 🎓 Заключение

Вы создали **профессиональный MSI установщик** со всеми необходимыми функциями:

✅ Выбор компонентов при установке
✅ Автоматическая установка Windows Service
✅ Автоматическая регистрация Shell Extension  
✅ Автоматическое удаление всех компонентов
✅ Поддержка массового развертывания
✅ Подробное логирование
✅ Обработка ошибок

Установщик готов к использованию в продакшене!
