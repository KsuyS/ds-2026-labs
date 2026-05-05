using StackExchange.Redis;

namespace Valuator.Services;

public class RedisConnections
{
    public IConnectionMultiplexer Main { get; }
    public IConnectionMultiplexer Ru { get; }
    public IConnectionMultiplexer Eu { get; }
    public IConnectionMultiplexer Asia { get; }

    public RedisConnections(
        IConnectionMultiplexer main,
        IConnectionMultiplexer ru,
        IConnectionMultiplexer eu,
        IConnectionMultiplexer asia)
    {
        Main = main;
        Ru = ru;
        Eu = eu;
        Asia = asia;
    }
}