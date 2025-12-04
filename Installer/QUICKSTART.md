# Быстрое руководство по MSI установщику

## 📦 Сборка установщика

### Шаг 1: Установите WiX Toolset

```powershell
# Установка WiX через .NET CLI
dotnet tool install --global wix --version 4.0.4

# Проверка установки
wix --version
```

### Шаг 2: Соберите MSI

```powershell
# Из корня проекта
cd c:\monitoring_polzovateley

# Запустите скрипт сборки
.\Scripts\Build-Installer.ps1

# Результат будет в: Installer\bin\Release\FileMonitorSetup.msi
```

---

## 💿 Установка

### Для файлового СЕРВЕРА:

```powershell
# Установка с GUI (выбор компонентов)
msiexec /i FileMonitorSetup.msi

# ИЛИ тихая установка только сервера
msiexec /i FileMonitorSetup.msi ADDLOCAL=ServerFeature /qn
```

**Что установится:**
- Служба FileMonitorService
- Автоматический запуск службы
- REST API на портах 5000 (HTTP) и 5001 (HTTPS)

### Для КЛИЕНТСКИХ компьютеров:

```powershell
# Установка с GUI
msiexec /i FileMonitorSetup.msi

# ИЛИ тихая установка только клиента
msiexec /i FileMonitorSetup.msi ADDLOCAL=ClientFeature /qn
```

**Что установится:**
- Shell Extension
- Пункт "Кто использует файл?" в контекстном меню
- Автоматический перезапуск Explorer

### Для установки ОБОИХ компонентов:

```powershell
# GUI с выбором
msiexec /i FileMonitorSetup.msi

# Тихая установка обоих
msiexec /i FileMonitorSetup.msi /qn
```

---

## 🗑️ Удаление

### Через панель управления:

1. Откройте "Программы и компоненты" (`appwiz.cpl`)
2. Найдите "File Monitor System"
3. Нажмите "Удалить"

### Через командную строку:

```powershell
# Тихое удаление
msiexec /x FileMonitorSetup.msi /qn

# С интерфейсом
msiexec /x FileMonitorSetup.msi
```

**Что удалится автоматически:**
- ✅ Остановка и удаление Windows Service (сервер)
- ✅ Отмена регистрации Shell Extension (клиент)
- ✅ Удаление всех файлов
- ✅ Очистка реестра

---

## 🌐 Массовое развертывание

### Через Group Policy (GPO):

1. Скопируйте MSI на сетевую папку: `\\server\share\FileMonitorSetup.msi`
2. Откройте Group Policy Management
3. Создайте или отредактируйте GPO
4. Перейдите: Computer Configuration → Policies → Software Settings → Software Installation
5. Добавьте пакет с параметрами:
   - Для серверов: `ADDLOCAL=ServerFeature`
   - Для клиентов: `ADDLOCAL=ClientFeature`

### Через PowerShell на много компьютеров:

```powershell
# Установка сервера на файловые серверы
$servers = "FileServer01", "FileServer02"
foreach ($server in $servers) {
    Invoke-Command -ComputerName $server -ScriptBlock {
        msiexec /i "\\share\FileMonitorSetup.msi" ADDLOCAL=ServerFeature /qn /l*v "C:\Temp\install.log"
    }
}

# Установка клиента на все рабочие станции
$computers = Get-ADComputer -Filter {OperatingSystem -like "*Windows 10*"} | Select-Object -ExpandProperty Name
foreach ($pc in $computers) {
    Invoke-Command -ComputerName $pc -ScriptBlock {
        msiexec /i "\\share\FileMonitorSetup.msi" ADDLOCAL=ClientFeature /qn
    }
}
```

### Через SCCM/ConfigMgr:

1. Создайте Application в SCCM
2. Укажите установку: `msiexec /i FileMonitorSetup.msi ADDLOCAL=ClientFeature /qn`
3. Укажите удаление: `msiexec /x FileMonitorSetup.msi /qn`
4. Разверните на целевые коллекции

---

## ✅ Проверка установки

### Проверка сервера:

```powershell
# Проверка службы
Get-Service FileMonitorService

# Проверка API
Invoke-RestMethod -Uri "http://localhost:5000/api/files/health"

# Проверка файлов
dir "C:\Program Files\File Monitor System\Service"
```

### Проверка клиента:

```powershell
# Проверка Shell Extension в реестре
reg query "HKCR\*\shellex\ContextMenuHandlers" /s | findstr "FileMonitor"

# Проверка файлов
dir "C:\Program Files\File Monitor System\Client"

# Визуальная проверка
# 1. Откройте Explorer
# 2. ПКМ на любом файле
# 3. Должен быть пункт "Кто использует файл?"
```

---

## 🐛 Устранение неполадок

### Проблема: MSI не собирается

**Решение:**
```powershell
# Убедитесь, что WiX установлен
wix --version

# Переустановите WiX
dotnet tool uninstall --global wix
dotnet tool install --global wix --version 4.0.4

# Очистите и пересоберите
.\Scripts\Build-Installer.ps1 -Clean
```

### Проблема: Служба не запускается после установки

**Решение:**
```powershell
# Проверьте логи
Get-Content "C:\Program Files\File Monitor System\Service\logs\service-*.txt" -Tail 50

# Запустите вручную
sc start FileMonitorService

# Проверьте .NET Runtime
dotnet --list-runtimes
```

### Проблема: Shell Extension не появляется

**Решение:**
```powershell
# Перезапустите Explorer
taskkill /f /im explorer.exe
explorer.exe

# Или перезагрузите компьютер
Restart-Computer
```

### Проблема: Ошибка при удалении

**Решение:**
```powershell
# Остановите службу вручную
sc stop FileMonitorService

# Повторите удаление
msiexec /x FileMonitorSetup.msi /qn

# Если не помогло, удалите вручную
sc delete FileMonitorService
regasm "C:\Program Files\File Monitor System\Client\FileMonitorClient.dll" /unregister
```

---

## 📝 Параметры командной строки

### Параметры msiexec:

| Параметр | Описание |
|----------|----------|
| `/i` | Установка |
| `/x` | Удаление |
| `/qn` | Тихий режим (без UI) |
| `/qb` | Базовый UI (только прогресс) |
| `/l*v file.log` | Подробное логирование |

### Параметры установщика:

| Параметр | Описание |
|----------|----------|
| `ADDLOCAL=ServerFeature` | Установить только сервер |
| `ADDLOCAL=ClientFeature` | Установить только клиент |
| `ADDLOCAL=ALL` | Установить все (по умолчанию) |

### Примеры:

```powershell
# Тихая установка сервера с логом
msiexec /i FileMonitorSetup.msi ADDLOCAL=ServerFeature /qn /l*v server_install.log

# Базовый UI для клиента
msiexec /i FileMonitorSetup.msi ADDLOCAL=ClientFeature /qb

# Установка обоих с логом
msiexec /i FileMonitorSetup.msi /qn /l*v full_install.log

# Удаление с логом
msiexec /x FileMonitorSetup.msi /qn /l*v uninstall.log
```

---

## 🎯 Сценарии использования

### Сценарий 1: Малый офис (1 сервер, 10 клиентов)

```powershell
# На сервере
msiexec /i FileMonitorSetup.msi ADDLOCAL=ServerFeature

# На каждом клиенте (вручную)
msiexec /i FileMonitorSetup.msi ADDLOCAL=ClientFeature
```

### Сценарий 2: Средний офис (GPO развертывание)

1. Создайте сетевую папку `\\dc\netlogon\FileMonitor\`
2. Скопируйте MSI туда
3. Создайте 2 GPO:
   - GPO для серверов → `ADDLOCAL=ServerFeature`
   - GPO для клиентов → `ADDLOCAL=ClientFeature`

### Сценарий 3: Крупная организация (SCCM)

1. Импортируйте MSI в SCCM
2. Создайте 2 приложения:
   - File Monitor Server
   - File Monitor Client
3. Разверните на соответствующие коллекции

---

## 📞 Поддержка

Если установщик не работает:

1. ✅ Проверьте права администратора
2. ✅ Проверьте логи установки: `msiexec /i ... /l*v install.log`
3. ✅ Проверьте Event Viewer → Application log
4. ✅ Используйте PowerShell скрипты как альтернативу: `.\Scripts\Install-Service.ps1`
