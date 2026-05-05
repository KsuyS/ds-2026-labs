using NATS.Client;
using System;
using System.Text;

namespace Consumer;

class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine("Consumer started");

        ConnectionFactory connectionFactory = new ConnectionFactory();
        using IConnection connection = connectionFactory.CreateConnection();

        IAsyncSubscription subscription = RunConsumer(connection);
        subscription.Start();

        Console.WriteLine("Press Enter to exit");
        Console.ReadLine();
        subscription.Unsubscribe();

        connection.Drain();
        connection.Close();
    }

    private static IAsyncSubscription RunConsumer(IConnection connection)
    {
        return connection.SubscribeAsync(
            subject: "valuator.processing.rank",
            queue: "rank_calculator",
            handler: (_, args) =>
            {
                string m = Encoding.UTF8.GetString(args.Message.Data);
                Console.WriteLine("Consuming: {0} from subject {1}", m, args.Message.Subject);
            }
        );
    }
}