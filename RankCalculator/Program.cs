using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using StackExchange.Redis;

namespace RankCalculator;

class Program
{
    private const string QueueName = "valuator.processing.rank";

    private static ConnectionMultiplexer _mainRedis = null!;
    private static readonly Dictionary<string, ConnectionMultiplexer> _shards = new();

    public static async Task Main(string[] args)
    {
        Console.WriteLine("RankCalculator started");

        var mainConn = Environment.GetEnvironmentVariable("DB_MAIN") ?? "localhost:6000";
        var ruConn = Environment.GetEnvironmentVariable("DB_RU") ?? "localhost:6001";
        var euConn = Environment.GetEnvironmentVariable("DB_EU") ?? "localhost:6002";
        var asiaConn = Environment.GetEnvironmentVariable("DB_ASIA") ?? "localhost:6003";

        _mainRedis = ConnectionMultiplexer.Connect(mainConn);
        _shards["RU"] = ConnectionMultiplexer.Connect(ruConn);
        _shards["EU"] = ConnectionMultiplexer.Connect(euConn);
        _shards["ASIA"] = ConnectionMultiplexer.Connect(asiaConn);

        var factory = new ConnectionFactory { HostName = "localhost" };
        await using IConnection connection = await factory.CreateConnectionAsync();
        await using IChannel channel = await connection.CreateChannelAsync();

        await DeclareTopologyAsync(channel);

        string consumerTag = await RunConsumer(channel);

        Console.WriteLine("Press [Enter] to exit");
        Console.ReadLine();

        await channel.BasicCancelAsync(consumerTag);
    }

    private static async Task<string> RunConsumer(IChannel channel)
    {
        AsyncEventingBasicConsumer consumer = new(channel);
        consumer.ReceivedAsync += async (_, eventArgs) =>
        {
            await ConsumeAsync(channel, eventArgs);
        };

        return await channel.BasicConsumeAsync(
            queue: QueueName,
            autoAck: false,
            consumer: consumer
        );
    }

    private static async Task ConsumeAsync(IChannel channel, BasicDeliverEventArgs eventArgs)
    {
        try
        {
            string id = Encoding.UTF8.GetString(eventArgs.Body.ToArray());
            if (string.IsNullOrWhiteSpace(id))
            {
                Console.WriteLine("Пустой Id в сообщении");
                await channel.BasicNackAsync(eventArgs.DeliveryTag, false, false);
                return;
            }

            var mainDb = _mainRedis.GetDatabase();
            var region = await mainDb.StringGetAsync($"SHARD:{id}");

            if (region.IsNullOrEmpty)
            {
                Console.WriteLine($"Регион не найден для {id}");
                await channel.BasicNackAsync(eventArgs.DeliveryTag, false, false);
                return;
            }

            Console.WriteLine($"LOOKUP: {id}, {region}");

            var shardRedis = _shards[region.ToString()];
            var shardDb = shardRedis.GetDatabase();
            var text = await shardDb.StringGetAsync($"TEXT:{id}");

            if (text.IsNullOrEmpty)
            {
                Console.WriteLine($"Текст не найден для {id} в шарде {region}");
                await channel.BasicNackAsync(eventArgs.DeliveryTag, false, false);
                return;
            }

            var interval = TimeSpan.FromSeconds(new Random().Next(3, 16));
            Console.WriteLine($"Waiting {interval} for text-{id}");
            await Task.Delay(interval);

            double rank = CalculateRank(text.ToString());

            await shardDb.StringSetAsync($"RANK:{id}", rank.ToString());
            Console.WriteLine($"Rank={rank:F2} для {id} в шарде {region}");

            await NotifyValuator(id, rank);
            await PublishRankCalculated(id, rank);

            await channel.BasicAckAsync(eventArgs.DeliveryTag, false);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка: {ex.Message}");
            await channel.BasicNackAsync(eventArgs.DeliveryTag, false, false);
        }
    }

    private static async Task NotifyValuator(string textId, double rank)
    {
        try
        {
            using var client = new HttpClient();
            var data = new
            {
                textId,
                rank
            };
            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            Console.WriteLine($"POST http://localhost:8080/api/notify");
            var response = await client.PostAsync("http://localhost:8080/api/notify", content);
            Console.WriteLine($"EventsLogger → Valuator: {response.StatusCode}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"NotifyValuator error: {ex.Message}");
        }
    }

    private static async Task PublishRankCalculated(string textId, double rank)
    {
        try
        {
            var factory = new ConnectionFactory
            {
                HostName = "localhost",
            };

            await using IConnection connection = await factory.CreateConnectionAsync();
            await using IChannel publishChannel = await connection.CreateChannelAsync();

            await publishChannel.ExchangeDeclareAsync(
                exchange: "rank.calculated",
                type: ExchangeType.Fanout
            );

            var eventData = new
            {
                EventType = "RankCalculated",
                TextId = textId,
                Rank = rank,
            };

            var message = JsonSerializer.Serialize(eventData);
            var body = Encoding.UTF8.GetBytes(message);

            await publishChannel.BasicPublishAsync(
                exchange: "rank.calculated",
                routingKey: "",
                body: body
            );

            Console.WriteLine($"Отправлено RankCalculated: {textId} = {rank:F2}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка публикации: {ex.Message}");
        }
    }

    private static async Task DeclareTopologyAsync(IChannel channel)
    {
        await channel.QueueDeclareAsync(
            queue: QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false
        );
    }

    private static double CalculateRank(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0;
        }

        int totalChars = text.Length;
        int alphaChars = 0;

        foreach (char c in text)
        {
            if (IsAlpha(c)) alphaChars++;
        }

        return (double)(totalChars - alphaChars) / totalChars;
    }

    private static bool IsAlpha(char c)
    {
        return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') ||
               (c >= 'а' && c <= 'я') || (c >= 'А' && c <= 'Я') ||
               (c == 'ё' || c == 'Ё');
    }
}