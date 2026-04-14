using NATS.Client;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Producer;

class Program
{
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
        // Установка соединения с NATS по адресу localhost:4222
        ConnectionFactory connectionFactory = new ConnectionFactory();
        using IConnection connection = connectionFactory.CreateConnection();

        // Отправка сообщения ежесекундно в цикле.
        ulong count = 0;
        while (!ct.IsCancellationRequested)
        {
            string message = $"#{count}";
            byte[] messageData = Encoding.UTF8.GetBytes(message);

            Console.WriteLine($"Sending message: {message}");
            connection.Publish(
                "valuator.processing.rank",
                messageData
            );

            await Task.Delay(TimeSpan.FromSeconds(1), ct);
            ++count;
        }

        await connection.DrainAsync();
        connection.Close();
    }
}