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

        QueueDeclareOk queueDeclareResult = await channel.QueueDeclareAsync();
        string queueName = queueDeclareResult.QueueName;

        foreach (var exchange in EventExchanges)
        {
            await channel.ExchangeDeclareAsync(
                exchange: exchange, 
                type: ExchangeType.Fanout
            );
            await channel.QueueBindAsync(
                queue: queueName,
                exchange: exchange,
                routingKey: ""
            );
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

                await channel.BasicAckAsync(
                    deliveryTag: eventArgs.DeliveryTag,
                    multiple: false
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"EventLogger error: {ex.Message}");
                await channel.BasicNackAsync(
                    deliveryTag: eventArgs.DeliveryTag,
                    multiple: false,
                    requeue: true
                );
            }
        };

        await channel.BasicConsumeAsync(
            queue: queueName,
            autoAck: false,
            consumer: consumer
        );
        Console.ReadLine();
    }
}