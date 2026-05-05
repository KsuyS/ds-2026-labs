using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using StackExchange.Redis;
using System.Text;
using System.Text.Json;
using Valuator.Services;

namespace Valuator.Pages;

public class IndexModel : PageModel
{
    private const string ExchangeName = "valuator.processing.rank";
    private const string QueueName = "valuator.processing.rank";

    private readonly ILogger<IndexModel> _logger;
    private readonly RedisShardService _shardService;

    [BindProperty]
    public string Text { get; set; } = "";

    [BindProperty]
    public string Country { get; set; } = "";

    public IndexModel(
        ILogger<IndexModel> logger,
        RedisShardService shardService
    )
    {
        _logger = logger;
        _shardService = shardService;
    }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPost()
    {
        if (string.IsNullOrWhiteSpace(Text) || string.IsNullOrWhiteSpace(Country))
        {
            return Redirect("/error");
        }

        _logger.LogDebug(Text);
        string id = Guid.NewGuid().ToString();

        await _shardService.SaveTextAsync(id, Text, Country);

        var shardDb = await _shardService.GetShardDbAsync(id);
        if (shardDb != null)
        {
            double similarity = await CalculateSimilarity(shardDb, Text, id);
            await shardDb.StringSetAsync($"SIMILARITY:{id}", similarity.ToString());
            _logger.LogInformation($"Similarity={similarity:F2} для {id}");
            await PublishSimilarityCalculated(id, similarity);
        }

        await SendToRabbitMQ(id);

        return Redirect($"summary?id={id}");
    }

    private async Task<double> CalculateSimilarity(IDatabase db, string currentText, string currentId)
    {
        var server = db.Multiplexer.GetServer(db.Multiplexer.GetEndPoints().First());
        var keys = server.Keys(pattern: "TEXT:*").ToList();

        foreach (var key in keys)
        {
            var keyStr = key.ToString();
            if (keyStr == $"TEXT:{currentId}")
            {
                continue;
            }

            var existingText = await db.StringGetAsync(keyStr);
            if (!existingText.IsNullOrEmpty && existingText.ToString() == currentText)
            {
                return 1.0;
            }
        }

        return 0.0;
    }

    private async Task PublishSimilarityCalculated(string textId, double similarity)
    {
        try
        {
            var factory = new ConnectionFactory { HostName = "localhost" };
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

    private async Task SendToRabbitMQ(string id)
    {
        var factory = new ConnectionFactory() { HostName = "localhost" };
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
            type: ExchangeType.Direct
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
}