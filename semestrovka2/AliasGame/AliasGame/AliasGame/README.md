#  Alias Game

Многопользовательская игра "Элиас" (Alias) с клиент-серверной архитектурой на C#.

##  Описание

Alias - командная игра, в которой игроки объясняют слова своим товарищам по команде без использования однокоренных слов. Проект реализует:

- **TCP сервер** с кастомным бинарным протоколом (XProtocol)
- **WinForms клиент** с полнофункциональным UI
- **PostgreSQL база данных** для хранения пользователей, слов и статистики
- **Многопоточная обработка** подключений и игровых сессий

##  Архитектура

```
AliasGame/
├── Shared/                 # Общая библиотека
│   ├── Protocol/           # XProtocol - кастомный TCP протокол
│   │   ├── XPacket.cs      # Основной класс пакета
│   │   ├── XPacketConverter.cs  # Сериализация/десериализация
│   │   ├── XProtocolEncryptor.cs # Шифрование AES
│   │   └── Packets/        # Классы пакетов для каждого типа сообщений
│   ├── Models/             # Модели данных (Lobby, Player, Team, etc.)
│   └── ORM/                # Entity Framework DbContext и репозитории
├── Server/                 # TCP сервер
│   ├── Network/            # TcpGameServer, SessionManager
│   ├── Game/               # LobbyManager, GameManager
│   └── Handlers/           # Обработчики пакетов
├── Client/                 # WinForms клиент
│   ├── Network/            # GameClient, GameStateManager
│   └── Forms/              # UI формы
├── Database/               # SQL скрипты
└── Tests/                  # Юнит-тесты
```

##  Требования

- .NET 8.0 SDK
- PostgreSQL 14+
- Windows (для клиента WinForms)

##  Запуск

### Вариант 1: Docker (рекомендуется)

```bash
# Запуск сервера и базы данных
docker-compose up -d

# С админ-панелью pgAdmin
docker-compose --profile admin up -d
```

Сервер будет доступен на `localhost:7777`
pgAdmin: `http://localhost:5050` (admin@alias.game / admin123)

### Вариант 2: Ручной запуск

#### 1. Настройка базы данных

```bash
# Создание базы данных
psql -U postgres -c "CREATE DATABASE alias_game;"
psql -U postgres -c "CREATE USER alias_user WITH PASSWORD 'alias_password';"
psql -U postgres -c "GRANT ALL PRIVILEGES ON DATABASE alias_game TO alias_user;"

# Применение схемы и данных
psql -U alias_user -d alias_game -f Database/01_schema.sql
psql -U alias_user -d alias_game -f Database/02_seed_words_part1.sql
psql -U alias_user -d alias_game -f Database/03_seed_words_part2.sql
```

#### 2. Сборка проекта

```bash
dotnet restore
dotnet build
```

#### 3. Запуск сервера

```bash
cd Server
dotnet run
```

#### 4. Запуск клиента

```bash
cd Client
dotnet run
```

##  Игровой процесс

### Лобби
1. Игроки присоединяются к лобби
2. Распределяются по командам (перетаскивание)
3. Хост настраивает параметры игры
4. Хост запускает игру

### Настройки
-  Время раунда (10-300 сек)
-  Количество раундов (1-50)
-  Очков до победы (10-200)
-  Время на последнее слово (0-60 сек)
-  Ручное изменение очков
-  Передача хода хостом

### Игра
1. Объясняющий видит слово
2. Нажимает "Угадано" или "Пропуск"
3. По истечении времени - фаза последнего слова
4. Побеждает команда, набравшая больше очков

##  Протокол XProtocol

### Структура пакета
```
[Header: 3 bytes] [Type: 1 byte] [Subtype: 1 byte] [Fields...] [Ending: 2 bytes]
```

- **Header**: `0xAF 0xAA 0xAF` (обычный) или `0x95 0xAA 0xFF` (зашифрованный)
- **Ending**: `0xFF 0x00`

### Типы пакетов
- `1.x` - Аутентификация (Handshake, Login, Register)
- `2.x` - Управление лобби
- `3.x` - Управление командами
- `4.x` - Чат
- `5.x` - Настройки
- `6.x` - Игровой процесс
- `7.x` - Геймплей
- `9.x` - Админ-панель

##  Тестирование

```bash
# Запуск тестов
dotnet test

# С покрытием кода
dotnet test --collect:"XPlat Code Coverage"
```

##  База данных

### Таблицы
- `users` - пользователи и статистика
- `categories` - категории слов
- `words` - словарь (500+ слов)
- `game_history` - история игр
- `game_settings_presets` - пресеты настроек

### Стандартные учётные данные
- **Админ**: admin / admin123

##  Конфигурация

### Server/appsettings.json
```json
{
  "Server": {
    "Host": "0.0.0.0",
    "Port": 7777,
    "MaxConnections": 100
  },
  "Database": {
    "ConnectionString": "Host=localhost;Port=5432;Database=alias_game;..."
  }
}
```

### Client/appsettings.json
```json
{
  "Server": {
    "Host": "127.0.0.1",
    "Port": 7777
  }
}
```

##  Критерии оценки

| Критерий | Реализация |
|----------|------------|
| Архитектура клиент-сервер | ✅ SOLID, слоистая архитектура |
| ORM и работа с БД | ✅ EF Core, Unit of Work, Repository |
| TCP-протокол | ✅ Кастомный XProtocol с шифрованием |
| TCP-сервер | ✅ Многопоточность, управление сессиями |
| База данных | ✅ PostgreSQL, 500+ слов |
| Админ-панель | ✅ CRUD для слов, пользователей |
| Клиентский UI | ✅ WinForms с валидацией |
| Игровая логика | ✅ Таймер, очки, синхронизация |
| Docker | ✅ docker-compose для всего стека |
| Тесты | ✅ xUnit, FluentAssertions |

##  Лицензия

Учебный проект. MIT License.

##  Авторы

Разработано для курса сетевого программирования на C#.
