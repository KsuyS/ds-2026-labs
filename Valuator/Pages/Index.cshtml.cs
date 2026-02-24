using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Valuator.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly IDistributedCache _cache;

    public IndexModel(ILogger<IndexModel> logger, IDistributedCache cache)
    {
        _logger = logger;
        _cache = cache;
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

        string textKey = "TEXT-" + id;
        await _cache.SetStringAsync(textKey, text);

        string rankKey = "RANK-" + id;
        double rank = CalculateRank(text);
        await _cache.SetStringAsync(rankKey, rank.ToString());

        string similarityKey = "SIMILARITY-" + id;
        double similarity = await CalculateSimilarity(text, id);
        await _cache.SetStringAsync(similarityKey, similarity.ToString());

        await UpdateTextKeysList(textKey);

        return Redirect($"summary?id={id}");
    }

    private double CalculateRank(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0;
        }

        int totalChars = text.Length;
        int alphaChars = 0;

        foreach (char c in text)
        {
            if (IsAlpha(c))
            {
                alphaChars++;
            }
        }

        double rank = (double)(totalChars - alphaChars) / totalChars;

        return rank;
    }

    private bool IsAlpha(char c)
    {
        return (c >= 'a' && c <= 'z') ||
               (c >= 'A' && c <= 'Z') ||
               (c >= 'а' && c <= 'я') ||
               (c >= 'А' && c <= 'Я') ||
               (c == 'ё' || c == 'Ё');
    }

    private async Task<double> CalculateSimilarity(string currentText, string currentId)
    {
        var keysStr = await _cache.GetStringAsync("TEXT-KEYS");
        if (string.IsNullOrEmpty(keysStr))
        {
            return 0.0;
        }

        string[] allKeys = keysStr.Split(',');

        foreach (string key in allKeys)
        {
            if (string.IsNullOrWhiteSpace(key) || key == $"TEXT-{currentId}")
            {
                continue;
            }

            var existingText = await _cache.GetStringAsync(key);
            if (existingText == currentText)
            {
                return 1.0;
            }
        }

        return 0.0;
    }

    private async Task UpdateTextKeysList(string newTextKey)
    {
        var keysStr = await _cache.GetStringAsync("TEXT-KEYS") ?? "";
        var keyList = keysStr.Split(',')
                           .Where(k => !string.IsNullOrWhiteSpace(k))
                           .ToHashSet();

        keyList.Add(newTextKey);
        await _cache.SetStringAsync("TEXT-KEYS", string.Join(",", keyList));
    }
}
