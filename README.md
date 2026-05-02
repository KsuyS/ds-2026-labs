# PA7. Безопасность распределённой системы

**Цель:** научиться обеспечивать базовый уровень безопасности распределённой системы, включая сервисы и промежуточное ПО (СУБД, брокеры сообщений).

- Задание делается на базе задания PA4, PA5, PA6 (на выбор)

# Задание

Лабораторная содержит два задания. Для сдачи лабораторной вы должны выполнить оба.

## Задание 1. Аутентификация для промежуточного ПО

Защитите ПО промежуточного слоя с помощью аутентификации по паролю:

1. В настройках Redis нужно включить аутентификацию по паролю с заданным паролем
2. В настройках RabbitMQ нужно включить аутентификацию по паролю с заданным паролем
3. Все сервисы (компоненты) системы должны получать пароли как параметры
     - можно использовать файлы конфигурации, например `appsettings.json` и `appsettings.Development.json` для C#
     - можно использовать переменные окружения
     - нельзя хранить пароли в исходном коде приложения
     - файлы с паролями можно хранить в репозитории в открытом виде **только** при условии, что это пароли локального окружения разработчика

## Задание 2. Аутентификация пользователей системы

Добавьте регистрацию, идентификацию, аутентификацию и авторизацию пользователей системы:

1. (_Регистрация_) Пользователь может зарегистрироваться, используя уникальный логин и пароль
2. (_Аутентификация_) Пользователь может войти в систему, используя ранее указанные им логин и пароль
    - нет функций восстановления пароля, нет альтернативных способов аутентификации
3. (_Идентификация_) Для каждого отправленного на проверку текста хранится информация об авторе (ID или логин пользователя)
4. (_Авторизация_) Никто не может посмотреть результаты проверки (Summary), кроме автора текста

## Ссылки

### Аутентификация и авторизация пользователей

- C#: [Use cookie authentication without ASP.NET Core Identity](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/cookie?view=aspnetcore-9.0)
- C#: [Аутентификация с помощью куки](https://metanit.com/sharp/aspnet6/13.4.php)
- C#: [Авторизация на основе Claims](https://metanit.com/sharp/aspnet6/13.8.php)
- C#: [HttpContext.User, ClaimPrincipal и ClaimsIdentity](https://metanit.com/sharp/aspnet6/13.5.php)

### Аутентификация для промежуточного ПО

- Redis: [Redis: Password Authentication(AUTH) [Default User]](https://bigboxcode.com/redis-password-authentication-auth)
- Redis: [How to use redis username with password in docker-compose?](https://stackoverflow.com/questions/70807705/how-to-use-redis-username-with-password-in-docker-compose)
- RabbitMQ: [Credentials and Passwords](https://www.rabbitmq.com/docs/passwords)
- RabbitMQ: [How to add initial users when starting a RabbitMQ Docker container?](https://stackoverflow.com/questions/30747469/how-to-add-initial-users-when-starting-a-rabbitmq-docker-container)
