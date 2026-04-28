using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using StackExchange.Redis;
using System.Text;
using System.Text.Json;

namespace Valuator.Pages;

public class IndexModel : PageModel
{
    private const string ExchangeName = "valuator.processing.rank";
    private const string QueueName = "valuator.processing.rank";

    private readonly ILogger<IndexModel> _logger;
    private readonly IConnectionMultiplexer _redis;

    public IndexModel(ILogger<IndexModel> logger, IConnectionMultiplexer redis)
    {
        _logger = logger;
        _redis = redis;
    }

    public void OnGet()
    {

    }

    public async Task<IActionResult> OnPost(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return Redirect("/error");
        }

        _logger.LogDebug(text);
        string id = Guid.NewGuid().ToString();

        var db = _redis.GetDatabase();

        string textKey = "TEXT-" + id;
        await db.StringSetAsync(textKey, text);
        await UpdateTextKeysList(db, textKey);

        double similarity = await CalculateSimilarity(db, text, id);
        await db.StringSetAsync("SIMILARITY-" + id, similarity.ToString());

        _logger.LogInformation($"Similarity={similarity:F2} для {id}");

        await PublishSimilarityCalculated(id, similarity);

        await SendToRabbitMQ(id);

        return Redirect($"summary?id={id}");
    }

    private async Task PublishSimilarityCalculated(string textId, double similarity)
    {
        try
        {
            var factory = new ConnectionFactory
            {
                HostName = "localhost",
            };

            await using IConnection connection = await factory.CreateConnectionAsync();
            await using IChannel channel = await connection.CreateChannelAsync();

            await channel.ExchangeDeclareAsync(
                exchange: "similarity.calculated",
                type: ExchangeType.Fanout
            );

            var eventData = new
            {
                EventType = "SimilarityCalculated",
                TextId = textId,
                Similarity = similarity,
            };

            var message = JsonSerializer.Serialize(eventData);
            var body = Encoding.UTF8.GetBytes(message);

            await channel.BasicPublishAsync(
                exchange: "similarity.calculated",
                routingKey: "",
                body: body
            );

            _logger.LogInformation($"SimilarityCalculated: {textId} = {similarity:F2}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Similarity event error: {ex.Message}");
        }
    }

    private async Task<double> CalculateSimilarity(IDatabase db, string currentText, string currentId)
    {
        var keysStr = await db.StringGetAsync("TEXT-KEYS-LIST");

        if (keysStr.IsNullOrEmpty)
        {
            return 0.0;
        }

        string[] allKeys = keysStr.ToString().Split(',');
        foreach (string key in allKeys)
        {
            if (string.IsNullOrWhiteSpace(key) || key == $"TEXT-{currentId}")
            {
                continue;
            }

            var existingText = await db.StringGetAsync(key);
            if (!existingText.IsNullOrEmpty && existingText.ToString() == currentText)
            {
                return 1.0;
            }
        }

        return 0.0;
    }

    private async Task SendToRabbitMQ(string id)
    {
        var factory = new ConnectionFactory() 
        {
            HostName = "localhost",
        };
        await using IConnection connection = await factory.CreateConnectionAsync();
        await using IChannel channel = await connection.CreateChannelAsync();

        await DeclareTopologyAsync(channel);

        var body = Encoding.UTF8.GetBytes(id);

        await channel.BasicPublishAsync(
            exchange: ExchangeName,
            routingKey: "",
            mandatory: false,
            body: body
        );

        _logger.LogInformation($"Отправлено задание для {id}");
    }

    private static async Task DeclareTopologyAsync(RabbitMQ.Client.IChannel channel)
    {
        await channel.ExchangeDeclareAsync(
            exchange: ExchangeName,
            type: RabbitMQ.Client.ExchangeType.Direct
        );

        await channel.QueueDeclareAsync(
            queue: QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false
        );

        await channel.QueueBindAsync(
            queue: QueueName,
            exchange: ExchangeName,
            routingKey: ""
        );
    }

    private async Task UpdateTextKeysList(IDatabase db, string newTextKey)
    {
        var keysStr = await db.StringGetAsync("TEXT-KEYS-LIST");

        string[] keyList;
        if (keysStr.IsNullOrEmpty)
        {
            keyList = new[] { newTextKey };
        }
        else
        {
            keyList = keysStr.ToString()
                .Split(',')
                .Where(k => !string.IsNullOrWhiteSpace(k))
                .Append(newTextKey)
                .Distinct()
                .ToArray();
        }

        await db.StringSetAsync("TEXT-KEYS-LIST", string.Join(",", keyList));
    }
}
