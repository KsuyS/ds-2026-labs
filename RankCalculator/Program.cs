using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using StackExchange.Redis;

namespace RankCalculator;

class Program
{
    private const string QueueName = "valuator.processing.rank";

    private static readonly ConnectionMultiplexer redis = ConnectionMultiplexer.Connect("localhost:6379");

    public static async Task Main(string[] args)
    {
        Console.WriteLine("RankCalculator started");

        var factory = new ConnectionFactory 
        {
            HostName = "localhost",
        };

        await using IConnection connection = await factory.CreateConnectionAsync();
        await using IChannel channel = await connection.CreateChannelAsync();

        await DeclareTopologyAsync(channel);

        string consumerTag = await RunConsumer(channel);

        Console.WriteLine("Press [Enter] to exit");
        Console.ReadLine();

        await channel.BasicCancelAsync(consumerTag);
        Console.WriteLine("done");
    }

    private static async Task<string> RunConsumer(IChannel channel)
    {
        AsyncEventingBasicConsumer consumer = new (channel);
        consumer.ReceivedAsync += (_, eventArgs) => ConsumeAsync(channel, eventArgs);

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
            Console.WriteLine("Consuming");

            string id = Encoding.UTF8.GetString(eventArgs.Body.ToArray());
            if (string.IsNullOrWhiteSpace(id))
            {
                Console.WriteLine("Пустой Id в сообщении");
                await channel.BasicNackAsync(
                    eventArgs.DeliveryTag,
                    false,
                    false
                );
                return;
            }

            var db = redis.GetDatabase();
            string textKey = "TEXT-" + id;
            var text = await db.StringGetAsync(textKey);

            if (text.IsNullOrEmpty)
            {
                Console.WriteLine($"Текст не найден для {id}");
                await channel.BasicNackAsync(
                    eventArgs.DeliveryTag, 
                    false,
                    false
                );
                return;
            }

            double rank = CalculateRank(text.ToString());
            await db.StringSetAsync("RANK-" + id, rank.ToString());

            Console.WriteLine($"Rank={rank:F2} для {id}");

            await PublishRankCalculated(id, rank);

            await channel.BasicAckAsync(
                eventArgs.DeliveryTag,
                false
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{ex.Message}");
            await channel.BasicNackAsync(eventArgs.DeliveryTag, false, false);
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