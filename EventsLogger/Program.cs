using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

class Program
{
    private static readonly string[] EventExchanges = {
        "rank.calculated",
        "similarity.calculated",
    };

    static async Task Main()
    {
        Console.WriteLine("EventsLogger started");

        var factory = new ConnectionFactory
        {
            HostName = "localhost",
        };

        await using IConnection connection = await factory.CreateConnectionAsync();
        await using IChannel channel = await connection.CreateChannelAsync();

        string queueName = $"eventslogger-{Guid.NewGuid().ToString("N")[..8]}";

        await channel.QueueDeclareAsync(
            queue: queueName, 
            durable: false, 
            exclusive: true
        );

        foreach (var exchange in EventExchanges)
        {
            await channel.ExchangeDeclareAsync(exchange, ExchangeType.Fanout);
            await channel.QueueBindAsync(queueName, exchange, "");
        }

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, eventArgs) =>
        {
            try
            {
                var body = eventArgs.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                var eventData = JsonSerializer.Deserialize<JsonElement>(message);

                string eventType = eventData.GetProperty("EventType").GetString()!;
                string textId = eventData.GetProperty("TextId").GetString()!;

                Console.WriteLine($"{eventType}");
                Console.WriteLine($"ID: {textId}");

                if (eventType == "RankCalculated")
                {
                    double rank = eventData.GetProperty("Rank").GetDouble();
                    Console.WriteLine($"Rank: {rank:F2}");
                }
                else if (eventType == "SimilarityCalculated")
                {
                    double similarity = eventData.GetProperty("Similarity").GetDouble();
                    Console.WriteLine($"Similarity: {similarity:F2}");
                }

                Console.WriteLine();

                await channel.BasicAckAsync(eventArgs.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"EventLogger error: {ex.Message}");
                await channel.BasicNackAsync(eventArgs.DeliveryTag, false, true);
            }
        };

        await channel.BasicConsumeAsync(queueName, false, consumer);
        Console.ReadLine();
    }
}