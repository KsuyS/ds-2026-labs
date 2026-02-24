using System;
using Microsoft.Extensions.Caching.Distributed;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace Valuator.Pages;

public class SummaryModel : PageModel
{
    private readonly ILogger<SummaryModel> _logger;
    private readonly IDistributedCache _cache;

    public SummaryModel(ILogger<SummaryModel> logger, IDistributedCache cache)
    {
        _logger = logger;
        _cache = cache;
    }

    public double Rank { get; set; }
    public double Similarity { get; set; }

    public async Task OnGet(string id)
    {
        _logger.LogDebug(id);

        string rankKey = "RANK-" + id;
        string similarityKey = "SIMILARITY-" + id;

        var rankStr = await _cache.GetStringAsync(rankKey);
        var similarityStr = await _cache.GetStringAsync(similarityKey);

        Rank = double.TryParse(rankStr, out var rank) 
            ? rank 
            : 0.0;
        Similarity = double.TryParse(similarityStr, out var similarity) 
            ? similarity 
            : 0.0;
    }
}
