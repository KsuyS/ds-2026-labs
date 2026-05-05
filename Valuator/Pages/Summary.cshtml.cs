using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Valuator.Pages;

public class SummaryModel : PageModel
{
    private readonly ILogger<SummaryModel> _logger;
    private readonly IConnectionMultiplexer _redis;

    public SummaryModel(ILogger<SummaryModel> logger, IConnectionMultiplexer redis)
    {
        _logger = logger;
        _redis = redis;
    }

    public string Rank { get; set; } = "Оценка содержания не завершена";
    public string Similarity { get; set; } = "0.00";
    public bool IsReady => Rank != "Оценка содержания не завершена";

    public async Task OnGet(string? id)
    {
        if (string.IsNullOrEmpty(id))
        {
            Rank = "ошибка ID";
            return;
        }

        _logger.LogDebug(id);

        var db = _redis.GetDatabase();

        string rankKey = "RANK-" + id;
        string similarityKey = "SIMILARITY-" + id;

        var rankStr = await db.StringGetAsync(rankKey);
        var similarityStr = await db.StringGetAsync(similarityKey);

        Rank = !rankStr.IsNullOrEmpty && double.TryParse(rankStr.ToString(), out var rankParsed)
            ? rankParsed.ToString("F2")
            : "Оценка содержания не завершена";

        Similarity = !similarityStr.IsNullOrEmpty && double.TryParse(similarityStr.ToString(), out var similarityParsed)
            ? similarityParsed.ToString("F2")
            : "0.00";
    }
}
