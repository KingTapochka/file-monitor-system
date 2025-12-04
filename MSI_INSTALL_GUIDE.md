# MSI УСТАНОВЩИК - ИНСТРУКЦИЯ ПО ИСПОЛЬЗОВАНИЮ

## 🎯 Установка с GUI

MSI установщик поддерживает **стандартный Windows Installer GUI**, который показывает:
- Приветствие
- Выбор компонентов
- Прогресс установки
- Завершение

### Запуск установки:

```powershell
# Двойной клик на FileMonitorSetup.msi
# ИЛИ
msiexec /i FileMonitorSetup.msi
```

**GUI показывает:**
- ✅ Выбор компонентов (Server/Client)
- ✅ Прогресс установки
- ✅ Завершение установки

---

## ⚙️ Настройка сервера и порта

### Вариант 1: Параметры командной строки (РЕКОМЕНДУЕТСЯ)

```powershell
# Установка сервера с настройкой адреса и порта:
msiexec /i FileMonitorSetup.msi ADDLOCAL=ServerFeature SERVER_ADDRESS=192.168.1.100 SERVER_PORT=8080 /qb

# Установка клиента с указанием сервера:
msiexec /i FileMonitorSetup.msi ADDLOCAL=ClientFeature SERVER_ADDRESS=192.168.1.100 SERVER_PORT=8080 /qb

# Оба компонента:
msiexec /i FileMonitorSetup.msi ADDLOCAL=ServerFeature,ClientFeature SERVER_ADDRESS=localhost SERVER_PORT=5000 /qb
```

**Параметры:**
- `SERVER_ADDRESS` - адрес сервера (по умолчанию: localhost)
- `SERVER_PORT` - порт HTTP API (по умолчанию: 5000)
- `ADDLOCAL=ServerFeature` - установить только сервер
- `ADDLOCAL=ClientFeature` - установить только клиент
- `ADDLOCAL=ServerFeature,ClientFeature` - установить оба
- `/qb` - базовый GUI с прогресс-баром
- `/qn` - тихая установка без GUI

### Вариант 2: Ручная настройка после установки

После установки отредактируйте конфигурационные файлы:

**Для сервера:**
```powershell
# Файл: C:\Program Files\File Monitor System\Service\appsettings.json
notepad "C:\Program Files\File Monitor System\Service\appsettings.json"

# Измените:
{
  "Urls": "http://0.0.0.0:5000",  // Измените адрес и порт
  "Logging": { ... }
}

# Перезапустите службу:
Restart-Service FileMonitorService
```

**Для клиента:**
```powershell
# Файл: C:\Program Files\File Monitor System\Client\FileMonitorClient.dll.config
notepad "C:\Program Files\File Monitor System\Client\FileMonitorClient.dll.config"

# Измените:
<add key="ApiBaseUrl" value="http://192.168.1.100:5000" />

# Перезапустите Explorer:
taskkill /f /im explorer.exe
start explorer.exe
```

---

## 📋 Примеры использования

### Установка на файловый сервер (Windows Server 2019):

```powershell
# С GUI:
msiexec /i FileMonitorSetup.msi

# Тихая установка на порту 8080:
msiexec /i FileMonitorSetup.msi ADDLOCAL=ServerFeature SERVER_PORT=8080 /qn
```

### Установка на рабочие станции (Windows 10):

```powershell
# Установка клиента с указанием сервера:
msiexec /i FileMonitorSetup.msi ADDLOCAL=ClientFeature SERVER_ADDRESS=fileserver.domain.local SERVER_PORT=5000 /qb
```

### Массовое развертывание через GPO:

1. **Разместите MSI на сетевой папке:**
```powershell
Copy-Item FileMonitorSetup.msi \\domain\netlogon\FileMonitor\
```

2. **Создайте GPO:**
- Computer Configuration → Software Settings → Software Installation
- Добавьте: `\\domain\netlogon\FileMonitor\FileMonitorSetup.msi`

3. **Настройте параметры установки:**
- Properties → Modifications → Transforms
- ИЛИ используйте командную строку с параметрами:
  ```
  msiexec /i \\domain\netlogon\FileMonitor\FileMonitorSetup.msi ADDLOCAL=ClientFeature SERVER_ADDRESS=fileserver SERVER_PORT=5000 /qn
  ```

---

## 🔧 Проверка установки

```powershell
# Проверка установленных компонентов:
Get-WmiObject -Class Win32_Product | Where-Object { $_.Name -like "*File Monitor*" }

# Проверка службы:
Get-Service FileMonitorService
sc.exe qc FileMonitorService

# Проверка файлов:
Get-ChildItem "C:\Program Files\File Monitor System"

# Проверка API:
Invoke-RestMethod http://localhost:5000/api/files/health

# Проверка клиента:
# ПКМ на файле → "Кто использует файл?"
```

---

## 🗑️ Удаление

```powershell
# С GUI:
msiexec /x FileMonitorSetup.msi

# Тихое удаление:
msiexec /x FileMonitorSetup.msi /qn

# Через Панель управления:
# Панель управления → Программы и компоненты → File Monitor System → Удалить
```

---

## ⚠️ Важные замечания

1. **Требуются права администратора** для установки и удаления
2. **Служба не запустится автоматически** - требуется:
   ```powershell
   Start-Service FileMonitorService
   # ИЛИ перезагрузка компьютера
   ```
3. **Shell Extension требует перезапуска Explorer:**
   ```powershell
   taskkill /f /im explorer.exe
   start explorer.exe
   ```
4. **При изменении SERVER_ADDRESS/SERVER_PORT** нужно пересоздать MSI или настроить вручную после установки

---

## 🚀 Быстрый старт для тестирования

```powershell
# 1. Соберите MSI (если еще не собран):
.\Scripts\Build-Installer.ps1

# 2. Установите локально с GUI:
msiexec /i .\Installer\FileMonitorSetup.msi

# 3. Выберите компоненты в GUI

# 4. После установки запустите службу от имени администратора:
Start-Service FileMonitorService

# 5. Проверьте:
Get-Service FileMonitorService
Invoke-RestMethod http://localhost:5000/api/files/health
```

---

## 📖 Дополнительная документация

- `README.md` - общее описание проекта
- `DEPLOYMENT.md` - развертывание через GPO/SCCM
- `QUICKSTART.md` - быстрый старт
- `START_INSTALL.md` - установка через PowerShell скрипты
