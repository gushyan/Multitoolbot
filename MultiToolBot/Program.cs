using Application.Profiles;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Services.Cache;
using TelegramStopBot.Logic;
using PermGorTrans.ApiClient;
using Serilog;
using Serilog.Events;
using Services.Classes;
using Services.Interfaces;
using Telegram.Bot;
using TelegramStopBot.Handlers;


Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        path: "logs/bot-log-.txt",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 7,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
    )
    .CreateLogger();
try
{
    Log.Information("Запуск приложения...");
    var builder = Host.CreateApplicationBuilder(args);

    builder.Services.AddSerilog();

    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("ConnectionString 'DefaultConnection' not found.");

    var botToken = builder.Configuration["BotSettings:Token"]
        ?? throw new InvalidOperationException("Bot Token not found in configuration.");

    var defaultUrl = builder.Configuration["GortransPerm:DefaultUrl"]
        ?? throw new InvalidOperationException("DefaultUrl not found in configuration."); ;

    builder.Services.AddDbContext<BusDbContext>(options =>
        options.UseNpgsql(connectionString));

    builder.Services.AddAutoMapper(cfg =>
    {
        cfg.AddMaps(typeof(FavStopsProfile).Assembly);
    });

    builder.Services.AddHttpClient<IPermGortransClient, PermGortransClient>(client =>
    {
        client.Timeout = TimeSpan.FromSeconds(25); 
        client.DefaultRequestHeaders.Add("User-Agent", "MultitoolBot/1.0");
        client.BaseAddress = new Uri(defaultUrl);
    });

    builder.Services.AddSingleton<IStopPlaceCache, StopPlaceCache>();

    builder.Services.AddSingleton<ITelegramBotClient>(new TelegramBotClient(botToken));

    builder.Services.AddScoped<IStopsTelegramFormatter, StopsTelegramFormatter>();

    builder.Services.AddScoped<IStopService, StopService>();

    builder.Services.AddScoped<IFavStopsService, FavStopsService>();

    builder.Services.AddScoped<CallbackHandler>();

    builder.Services.AddScoped<MessageHandler>();

    builder.Services.AddScoped<InlineHandler>();

    builder.Services.AddScoped<ExceptionHandler>();

    builder.Services.AddHostedService<TelegramUpdateHandler>();

    var host = builder.Build();

    using (var scope = host.Services.CreateScope())
    {

        var cache = scope.ServiceProvider.GetRequiredService<IStopPlaceCache>();

        await cache.InitializeAsync(CancellationToken.None);
    }

    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Приложение аварийно завершилось");
}
finally
{
    Log.CloseAndFlush();
}