# Git Repository Setup - File Monitor System

## Текущее состояние репозитория

Локальный git репозиторий создан со следующей структурой:

### Ветки:
- **master** (основная ветка) - рабочая версия MSI установщика
- **feature/custom-gui** - экспериментальная ветка с попыткой добавления пользовательских диалогов

### Коммиты:
1. **d4706e9** (master, tag: v1.0.0-working)
   - "Working MSI installer - simple version with standard Windows Installer GUI"
   - Рабочая версия со стандартным GUI Windows Installer
   - Поддержка параметров командной строки SERVER_ADDRESS и SERVER_PORT

2. **0d7b2ff** (feature/custom-gui)
   - "Attempt to add custom GUI dialogs with server/port configuration (WiX 4 syntax issues)"
   - Попытка добавить пользовательские диалоги (имеет ошибки синтаксиса WiX 4)

## Для push в удаленный репозиторий

### Шаг 1: Создайте репозиторий на GitHub/GitLab/Bitbucket
Например, на GitHub:
1. Зайдите на https://github.com
2. Нажмите "New repository"
3. Назовите репозиторий (например, "file-monitor-system")
4. Скопируйте URL репозитория

### Шаг 2: Добавьте remote и выполните push

```powershell
# Добавить удаленный репозиторий (замените URL на ваш)
git remote add origin https://github.com/ваш-username/file-monitor-system.git

# Push основной ветки с тегом
git push -u origin master
git push origin v1.0.0-working

# Push экспериментальной ветки
git push origin feature/custom-gui
```

### Альтернатива: Push всех веток и тегов одновременно

```powershell
# Push всех веток
git push --all origin

# Push всех тегов
git push --tags origin
```

## Команды для работы с репозиторием

### Переключение между версиями:

```powershell
# Перейти на рабочую версию
git checkout master

# Перейти на версию с пользовательским GUI (с ошибками)
git checkout feature/custom-gui

# Вернуться на тег рабочей версии
git checkout v1.0.0-working
```

### Просмотр изменений:

```powershell
# История коммитов
git log --all --graph --oneline --decorate

# Различия между ветками
git diff master feature/custom-gui

# Различия в конкретном файле
git diff master feature/custom-gui -- Installer/Product.wxs
```

## Описание версий

### v1.0.0-working (master)
**Что работает:**
- ✅ MSI установщик собирается без ошибок
- ✅ Стандартный Windows Installer GUI (выбор компонентов, прогресс)
- ✅ Установка серверной службы (FileMonitorService)
- ✅ Установка клиентского расширения (Shell Extension)
- ✅ Автоматическое создание/запуск/удаление службы
- ✅ Регистрация COM для Shell Extension
- ✅ Поддержка параметров: SERVER_ADDRESS, SERVER_PORT

**Использование:**
```powershell
# Интерактивная установка с GUI
msiexec /i FileMonitorSetup.msi

# Установка с параметрами
msiexec /i FileMonitorSetup.msi ADDLOCAL=ServerFeature SERVER_ADDRESS=192.168.1.100 SERVER_PORT=8080 /qb
```

### feature/custom-gui (экспериментальная)
**Изменения:**
- Добавлены 5 пользовательских диалогов (Welcome, Features, ServerConfig, VerifyReady, Cancel)
- Добавлены поля ввода для SERVER_ADDRESS и SERVER_PORT
- Добавлен CustomAction для обновления конфигурации

**Проблемы:**
- ❌ 18 ошибок компиляции WiX 4
- ❌ WiX 4 не поддерживает Condition как дочерний элемент Feature
- ❌ Publish элементы не могут содержать inner text в WiX 4
- ❌ Требуется полная переработка под синтаксис WiX 4

**Для исправления потребуется:**
- Изучить документацию WiX 4 по созданию пользовательских диалогов
- Переписать UI элементы с использованием правильного синтаксиса WiX 4
- Значительное время на тестирование

## Рекомендации

**Для production использования:**
- Используйте **master** ветку (v1.0.0-working)
- Настройка через параметры командной строки надежна и документирована
- MSI_INSTALL_GUIDE.md содержит подробные инструкции

**Для разработки пользовательского GUI:**
- Работайте в ветке **feature/custom-gui**
- Изучите официальную документацию WiX 4
- Рассмотрите использование WiX UI Extension Library

## Файлы для справки

- `Installer/Product.wxs.working` - рабочая версия (master)
- `Installer/Product.wxs.complex` - версия с пользовательским GUI (feature/custom-gui)
- `Installer/Product.wxs.backup` - резервная копия
- `MSI_INSTALL_GUIDE.md` - подробная документация по использованию
