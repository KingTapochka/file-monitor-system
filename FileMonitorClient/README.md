# FileMonitorClient - Shell Extension

Расширение контекстного меню Windows Explorer для отображения пользователей, открывших файл.

## Возможности

- ✅ Добавление пункта "Кто использует файл?" в контекстное меню
- ✅ Красивое WPF окно с таблицей пользователей
- ✅ HTTP клиент для связи с FileMonitorService
- ✅ Индикатор загрузки и обработка ошибок
- ✅ Поддержка Windows 10/11

## Требования

- Windows 10/11
- .NET Framework 4.8
- FileMonitorService должна быть запущена

## Установка

### Регистрация Shell Extension

```powershell
# Сборка проекта
cd FileMonitorClient
dotnet build -c Release

# Регистрация (требуются права администратора)
cd bin\Release\net48
regasm FileMonitorClient.dll /codebase

# Для 64-bit Windows также:
"%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\regasm.exe" FileMonitorClient.dll /codebase
```

### Использование ServerManager (рекомендуется)

```powershell
# Установка ServerManager
Install-Package SharpShell

# Регистрация
srm install FileMonitorClient.dll -codebase

# Проверка
srm query FileMonitorClient.dll
```

### Удаление

```powershell
# Через regasm
regasm FileMonitorClient.dll /unregister

# Через ServerManager
srm uninstall FileMonitorClient.dll

# Перезапуск Explorer
taskkill /f /im explorer.exe ; explorer.exe
```

## Использование

1. Запустите FileMonitorService на сервере
2. Установите FileMonitorClient на клиентских машинах
3. Откройте файл на сетевой папке
4. Нажмите правой кнопкой мыши на другом файле
5. Выберите "Кто использует файл?"
6. Увидите список пользователей

## Конфигурация

Адрес API сервера можно настроить в коде `FileMonitorApiClient.cs`:

```csharp
public FileMonitorApiClient(string baseUrl = "http://your-server:5000")
```

Или через конфигурационный файл (TODO).

## Структура проекта

```
FileMonitorClient/
├── FileMonitorContextMenu.cs    - Context Menu Handler
├── Models/
│   └── ApiModels.cs              - Модели данных API
├── Services/
│   └── FileMonitorApiClient.cs   - HTTP клиент
├── UI/
│   ├── FileUsersDialog.xaml      - WPF окно
│   └── FileUsersDialog.xaml.cs   - Логика окна
└── Properties/
    └── Resources.Designer.cs      - Ресурсы (иконки)
```

## Разработка

### Отладка Shell Extension

Shell Extensions сложно отлаживать, так как они загружаются в процесс Explorer.exe:

1. Добавьте `MessageBox.Show()` для отладки
2. Используйте `System.Diagnostics.Debugger.Launch()`
3. Проверяйте логи в Event Viewer

### Тестирование без установки

Невозможно - Shell Extensions должны быть зарегистрированы в реестре.

### Быстрая переустановка

```powershell
# Скрипт для быстрой переустановки
taskkill /f /im explorer.exe
srm uninstall FileMonitorClient.dll
dotnet build -c Release
srm install bin\Release\net48\FileMonitorClient.dll -codebase
explorer.exe
```

## Известные проблемы

- **Explorer зависает**: перезапустите Explorer через Task Manager
- **Пункт меню не появляется**: проверьте регистрацию через `regedit`
- **Ошибка подключения к API**: убедитесь, что FileMonitorService запущена

## Безопасность

- Shell Extension работает в контексте пользователя
- Не требует прав администратора для использования
- Требует права администратора только для установки

## TODO

- [ ] Добавить иконку для пункта меню
- [ ] Конфигурационный файл для адреса API
- [ ] Кэширование результатов на клиенте
- [ ] Локализация интерфейса
- [ ] Поддержка нескольких файлов одновременно
- [ ] MSI установщик

## Следующие шаги

1. ✅ Shell Extension создана
2. ✅ WPF UI реализован
3. ⏳ Создание MSI установщика
4. ⏳ Тестирование на разных версиях Windows
