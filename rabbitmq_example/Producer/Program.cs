using System.Text;
using RabbitMQ.Client;

namespace Producer;

class Program
{
    private const string ExchangeName = "valuator.processing.rank";
    private const string QueueName = "valuator.processing.rank";

    public static void Main(string[] args)
    {
        Console.WriteLine("Producer started");

        CancellationTokenSource cts = new CancellationTokenSource();
        Task.Factory.StartNew(() => ProduceAsync(cts.Token), cts.Token);

        Console.WriteLine("Press Enter to exit");
        Console.ReadLine();

        cts.Cancel();

        Console.WriteLine("done");
    }

    private static async Task ProduceAsync(CancellationToken ct)
    {
        // Установка соединения с RabbitMQ по адресу localhost:5672
        ConnectionFactory factory = new ConnectionFactory
        {
            HostName = "localhost"
        };
        await using IConnection connection = await factory.CreateConnectionAsync(ct);
        await using IChannel channel = await connection.CreateChannelAsync(null, ct);

        await DeclareTopologyAsync(channel, ct);

        // Отправка сообщения ежесекундно в цикле.
        ulong count = 0;
        while (!ct.IsCancellationRequested)
        {
            string message = $"#{count}";
            byte[] messageData = Encoding.UTF8.GetBytes(message);

            Console.WriteLine($"Sending message: {message}");
            await channel.BasicPublishAsync(
                exchange: ExchangeName,
                routingKey: "",
                mandatory: false,
                body: messageData,
                cancellationToken: ct
            );

            await Task.Delay(TimeSpan.FromSeconds(1), ct);
            ++count;
        }

        await connection.CloseAsync(ct);
    }

    /// <summary>
    ///  Определяет топологию: producer -> exchange -> queue -> consumer.
    ///  В нашем случае соответствие 1:1 между exchange и queue, а routing key не используется.
    /// </summary>
    private static async Task DeclareTopologyAsync(IChannel channel, CancellationToken ct)
    {
        await channel.ExchangeDeclareAsync(
            exchange: ExchangeName,
            type: ExchangeType.Direct,
            cancellationToken: ct
        );
        await channel.QueueDeclareAsync(
            queue: QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: ct
        );
        await channel.QueueBindAsync(
            queue: QueueName,
            exchange: ExchangeName,
            routingKey: "",
            cancellationToken: ct);
    }
}