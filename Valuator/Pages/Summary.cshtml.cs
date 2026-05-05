using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Valuator.Services;

namespace Valuator.Pages;

public class SummaryModel : PageModel
{
    private readonly ILogger<SummaryModel> _logger;
    private readonly RedisShardService _shardService;

    public SummaryModel(ILogger<SummaryModel> logger, RedisShardService shardService)
    {
        _logger = logger;
        _shardService = shardService;
    }

    public string Rank { get; set; } = "Оценка содержания не завершена";
    public string Similarity { get; set; } = "0.00";

    public async Task OnGet(string? id)
    {
        if (string.IsNullOrEmpty(id))
        {
            Rank = "ошибка ID";
            return;
        }

        _logger.LogDebug(id);

        var similarity = await _shardService.GetSimilarityAsync(id);
        Similarity = similarity ?? "0.00";

        var rank = await _shardService.GetRankAsync(id);
        Rank = rank ?? "Оценка содержания не завершена";
    }
}