# Пример передачи сообщений с помощью брокера NATS

Сборка и запуск с помощью .NET SDK и Docker:

```bash
# Сборка
dotnet restore
dotnet build

# Запуск NATS в docker
docker-compose up -d

# Запуск Consumer
dotnet run --no-build --project=Consumer/

# Запуск Producer
dotnet run --no-build --project=Producer/
```
