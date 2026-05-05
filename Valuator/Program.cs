using StackExchange.Redis;
using Valuator.Hubs;
using Valuator.Services;

namespace Valuator;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddRazorPages();
        builder.Services.AddSignalR();
        builder.Services.AddControllers();

        builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
            ConnectionMultiplexer.Connect(Environment.GetEnvironmentVariable("DB_MAIN") ?? "localhost:6000"));

        builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
            ConnectionMultiplexer.Connect(Environment.GetEnvironmentVariable("DB_RU") ?? "localhost:6001"));

        builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
            ConnectionMultiplexer.Connect(Environment.GetEnvironmentVariable("DB_EU") ?? "localhost:6002"));

        builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
            ConnectionMultiplexer.Connect(Environment.GetEnvironmentVariable("DB_ASIA") ?? "localhost:6003"));

        builder.Services.AddScoped<RedisShardService>(sp =>
        {
            var multiplexers = sp.GetServices<IConnectionMultiplexer>().ToList();

            if (multiplexers.Count < 4)
            {
                throw new Exception("Не все Redis подключения зарегистрированы");
            }

            var mainMux = multiplexers[0];
            var ruMux = multiplexers[1];
            var euMux = multiplexers[2];
            var asiaMux = multiplexers[3];

            return new RedisShardService(mainMux, ruMux, euMux, asiaMux);
        });

        var app = builder.Build();

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
        }

        app.UseStaticFiles();
        app.UseRouting();
        app.UseWebSockets();

        app.MapHub<NotificationHub>("/notificationHub");
        app.MapRazorPages();
        app.MapControllers();

        app.Run();
    }
}