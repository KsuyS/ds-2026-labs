using StackExchange.Redis;

namespace Valuator.Services;

public class RedisShardService
{
    private readonly IDatabase _mainDb;
    private readonly Dictionary<string, IDatabase> _shards = new();
    private static readonly Dictionary<string, string> CountryToRegion = new()
    {
        ["Russia"] = "RU",
        ["France"] = "EU",
        ["Germany"] = "EU",
        ["UAE"] = "ASIA",
        ["India"] = "ASIA"
    };

    public RedisShardService(
        IConnectionMultiplexer mainMux,
        IConnectionMultiplexer ruMux,
        IConnectionMultiplexer euMux,
        IConnectionMultiplexer asiaMux
    )
    {
        var mainEndpoint = mainMux.GetEndPoints().First();
        var ruEndpoint = ruMux.GetEndPoints().First();
        var euEndpoint = euMux.GetEndPoints().First();
        var asiaEndpoint = asiaMux.GetEndPoints().First();

        Console.WriteLine($"[RedisShardService] MAIN endpoint: {mainEndpoint}");
        Console.WriteLine($"[RedisShardService] RU endpoint: {ruEndpoint}");
        Console.WriteLine($"[RedisShardService] EU endpoint: {euEndpoint}");
        Console.WriteLine($"[RedisShardService] ASIA endpoint: {asiaEndpoint}");

        _mainDb = mainMux.GetDatabase();
        _shards["RU"] = ruMux.GetDatabase();
        _shards["EU"] = euMux.GetDatabase();
        _shards["ASIA"] = asiaMux.GetDatabase();
    }

    public async Task<IDatabase?> GetShardDbAsync(string textId)
    {
        var shardKey = await _mainDb.StringGetAsync($"SHARD:{textId}");
        if (shardKey.IsNullOrEmpty)
        {
            Console.WriteLine($"LOOKUP: {textId} - НЕ НАЙДЕН");
            return null;
        }

        Console.WriteLine($"LOOKUP: {textId}, {shardKey}");
        return _shards[shardKey.ToString()];
    }

    public async Task SaveTextAsync(string textId, string text, string country)
    {
        var region = CountryToRegion[country];

        await _mainDb.StringSetAsync($"SHARD:{textId}", region);
        Console.WriteLine($"SAVED SHARD:{textId} - {region} в MAINDB");

        var shardDb = _shards[region];
        await shardDb.StringSetAsync($"TEXT:{textId}", text);
        await shardDb.StringSetAsync($"SIMILARITY:{textId}", "0.00");

        Console.WriteLine($"SAVED: {textId} → {region}");
    }

    public async Task<string?> GetRankAsync(string textId)
    {
        var shardDb = await GetShardDbAsync(textId);
        if (shardDb == null)
        {
            return null;
        }

        var rank = await shardDb.StringGetAsync($"RANK:{textId}");
        return rank.IsNullOrEmpty 
            ? null 
            : rank.ToString();
    }

    public async Task<string?> GetSimilarityAsync(string textId)
    {
        var shardDb = await GetShardDbAsync(textId);
        if (shardDb == null)
        {
            return null;
        }

        var similarity = await shardDb.StringGetAsync($"SIMILARITY:{textId}");
        return similarity.IsNullOrEmpty 
            ? null 
            : similarity.ToString();
    }
}