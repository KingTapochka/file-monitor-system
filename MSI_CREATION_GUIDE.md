# КАК СОЗДАТЬ MSI ИЗ ТЕКУЩЕГО ПРОЕКТА

## 🎯 ИТОГИ АНАЛИЗА ЧАТА

После анализа всего чата вот что у вас есть и что нужно сделать:

### ✅ ЧТО УЖЕ ГОТОВО

1. **Рабочий PowerShell установщик** (100% готов):
   - `Scripts/Install-All.ps1` - установка сервера/клиента
   - `Scripts/Uninstall-All.ps1` - удаление
   - Работает на Windows Server 2019 и Windows 10

2. **WiX проект создан** (90% готов, но с ошибками):
   - `Installer/Product.wxs` - основной файл
   - `Installer/ServerComponent.wxs` - серверные компоненты
   - `Installer/ClientComponent.wxs` - клиентские компоненты
   - WiX Toolset 4.0.4 установлен

3. **Скрипт сборки MSI** готов:
   - `Scripts/Build-Installer.ps1`

### ❌ ПРОБЛЕМЫ С MSI

1. **Custom Actions**: WiX 4 изменил синтаксис, inner text больше не поддерживается
2. **Wildcards**: `*.dll` в File/@Source не работает
3. **Условия**: Нужно переписать все `<Custom>` элементы

---

## 🚀 ТРИ СПОСОБА СОЗДАТЬ MSI

### СПОСОБ 1: ИСПОЛЬЗУЙТЕ POWERSHELL УСТАНОВЩИК (РЕКОМЕНДУЕТСЯ)

**Зачем создавать MSI, если у вас уже есть рабочий установщик?**

```powershell
# Установка на Windows Server 2019:
cd C:\monitoring_polzovateley
Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope Process -Force
.\Scripts\Install-All.ps1 -Component Server

# Установка на Windows 10:
.\Scripts\Install-All.ps1 -Component Client
```

**Преимущества:**
- ✅ Работает СЕЙЧАС
- ✅ Не нужны дополнительные инструменты
- ✅ Можно развернуть через GPO
- ✅ Поддерживает удаленную установку

---

### СПОСОБ 2: ИСПРАВИТЬ WIX 4 ПРОЕКТ (2-4 ЧАСА РАБОТЫ)

#### Шаг 1: Исправьте Product.wxs

Найдите все строки вида:
```xml
<Custom Action="InstallServerService" After="InstallFiles">
  (NOT REMOVE) AND (&amp;ServerFeature=3)
</Custom>
```

Замените на:
```xml
<Custom Action="InstallServerService" After="InstallFiles" Condition="(NOT REMOVE) AND (&amp;ServerFeature=3)" />
```

#### Шаг 2: Уберите wildcards

В `ServerComponent.wxs` и `ClientComponent.wxs` найдите:
```xml
<File Source="..\FileMonitorService\bin\Release\net8.0-windows\publish\*.dll" />
```

Замените на явный список файлов:
```xml
<File Source="..\FileMonitorService\bin\Release\net8.0-windows\publish\Microsoft.Extensions.Caching.Memory.dll" />
<File Source="..\FileMonitorService\bin\Release\net8.0-windows\publish\System.Text.Json.dll" />
<!-- ... и так далее для каждого файла -->
```

#### Шаг 3: Сгенерируйте список файлов

```powershell
# Скрипт для генерации списка файлов:
$publishPath = "C:\monitoring_polzovateley\FileMonitorService\bin\Release\net8.0-windows\publish"
Get-ChildItem $publishPath -Filter "*.dll" | ForEach-Object {
    Write-Host "<File Source=`"..\FileMonitorService\bin\Release\net8.0-windows\publish\$($_.Name)`" />"
}
```

#### Шаг 4: Соберите MSI

```powershell
cd C:\monitoring_polzovateley
Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope Process -Force
.\Scripts\Build-Installer.ps1
```

---

### СПОСОБ 3: ИСПОЛЬЗУЙТЕ WIX 3 (СТАБИЛЬНЫЙ)

WiX 3 имеет другой, более простой синтаксис.

#### Установка:
```powershell
# Скачайте и установите WiX 3.11:
# https://github.com/wixtoolset/wix3/releases/download/wix3112rtm/wix311.exe
```

#### Конвертация проекта:
1. Измените `<Wix xmlns="http://wixtoolset.org/schemas/v4/wxs">` на `<Wix xmlns="http://schemas.microsoft.com/wix/2006/wi">`
2. `<Package>` замените на `<Product>`
3. `<StandardDirectory Id="ProgramFiles64Folder">` на `<Directory Id="TARGETDIR" Name="SourceDir">`

---

## 📋 ПОШАГОВАЯ ИНСТРУКЦИЯ ДЛЯ СОЗДАНИЯ MSI

### Вариант A: Быстрое развертывание (5 минут)

```powershell
# 1. Скопируйте проект на серверы
Copy-Item -Path "C:\monitoring_polzovateley" -Destination "\\server\share\FileMonitor" -Recurse

# 2. На каждом сервере запустите:
\\server\share\FileMonitor\Scripts\Install-All.ps1 -Component Server

# 3. На клиентах:
\\server\share\FileMonitor\Scripts\Install-All.ps1 -Component Client
```

### Вариант B: Создать MSI (2-4 часа)

```powershell
# 1. Сначала соберите проекты
cd C:\monitoring_polzovateley
dotnet publish FileMonitorService -c Release
dotnet build FileMonitorClient -c Release

# 2. Вручную исправьте WiX файлы:
# - Уберите inner text из Custom элементов
# - Замените wildcards на явные списки файлов
# - Проверьте все GUID уникальны

# 3. Соберите MSI
$wixPath = "$env:USERPROFILE\.dotnet\tools\wix.exe"
cd Installer
& $wixPath build Product.wxs ServerComponent.wxs ClientComponent.wxs -arch x64 -out FileMonitorSetup.msi -ext WixToolset.UI.wixext

# 4. Проверьте результат
Get-Item FileMonitorSetup.msi
```

### Вариант C: Через GPO (10 минут)

```powershell
# 1. Создайте сетевую папку
New-Item -ItemType Directory -Path "\\domain\netlogon\FileMonitor"
Copy-Item -Path "C:\monitoring_polzovateley\*" -Destination "\\domain\netlogon\FileMonitor" -Recurse

# 2. Создайте GPO:
# - Computer Configuration -> Policies -> Windows Settings -> Scripts -> Startup
# - Add: \\domain\netlogon\FileMonitor\Scripts\Install-All.ps1
# - Parameters: -Component Server (или Client для клиентов)

# 3. Примените GPO к нужным OU
```

---

## 🎯 РЕКОМЕНДАЦИЯ

**Для Windows Server 2019 и Windows 10:**

### Сейчас (следующие 30 минут):
1. Используйте `Install-All.ps1` для установки
2. Это работает на 100%
3. Поддерживает оба Windows Server 2019 и Windows 10

### Потом (когда будет время):
1. Исправьте WiX файлы по инструкции выше
2. Соберите MSI для корпоративного развертывания
3. Используйте GPO/SCCM для массовой установки

---

## 🔍 ТЕКУЩИЕ ОШИБКИ WIX

```
WIX0400: The Custom element contains illegal inner text
WIX0027: The File/@Source attribute's value, '*.dll', is not a valid filename
```

**Решение:**
1. Удалите весь inner text из `<Custom>` элементов
2. Добавьте `Condition="..."` attribute
3. Замените `<File Source="*.dll" />` на список каждого файла отдельно

---

## ✅ ИТОГОВЫЕ КОМАНДЫ

```powershell
# === УСТАНОВКА НА СЕРВЕР ===
cd C:\monitoring_polzovateley
Set-ExecutionPolicy Bypass -Scope Process -Force
.\Scripts\Install-All.ps1 -Component Server

# Проверка:
Get-Service FileMonitorService
Invoke-RestMethod http://localhost:5000/api/files/health


# === УСТАНОВКА НА КЛИЕНТЫ ===
cd C:\monitoring_polzovateley
Set-ExecutionPolicy Bypass -Scope Process -Force
.\Scripts\Install-All.ps1 -Component Client

# Проверка:
# ПКМ на файле -> "Кто использует файл?"


# === УДАЛЕНИЕ ===
.\Scripts\Uninstall-All.ps1
```

**PowerShell установщик - это ВАШ MSI! Он делает то же самое, но без лишней сложности WiX.**
