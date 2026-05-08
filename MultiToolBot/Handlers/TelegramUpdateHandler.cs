using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Telegram.Bot;
using Telegram.Bot.Types;
using MultitoolBot;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Polling;
using Multitoolbot.Handlers;
using Telegram.Bot.Types.InlineQueryResults;

public class TelegramUpdateHandler : BackgroundService
{
    private readonly ITelegramBotClient _botClient;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TelegramUpdateHandler> _logger;

    public TelegramUpdateHandler(ITelegramBotClient botClient, IServiceScopeFactory scopeFactory, ILogger<TelegramUpdateHandler> logger)
    {
        _botClient = botClient;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogDebug("Начато прослушивание");

        var receiverOptions = new ReceiverOptions()
        {
            DropPendingUpdates = true,
        };

         _botClient.StartReceiving(
            updateHandler: HandleUpdateAsync,
            errorHandler: HandlePollingError,
            receiverOptions: receiverOptions,
            cancellationToken: stoppingToken
        );

            await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var controller = scope.ServiceProvider.GetRequiredService<TelegramBot>();
        var exceptionHandler = scope.ServiceProvider.GetRequiredService<ExceptionHandler>();

        try
        {
            _logger.LogDebug("Определение типа обращения пользователя");
            if (update.Message is { } message)
            {
                await controller.HandleMessageAsync(message, ct);
            }
            else if (update.CallbackQuery is { } callbackQuery)
            {
                await controller.HandleCallbackAsync(callbackQuery, ct);
            }
            else if (update.InlineQuery is { } inlineQuery)
            {
                await controller.HandleInlineQueryAsync(inlineQuery, ct);
            }
        }
        catch (Exception ex)
        {
            await exceptionHandler.HandleAsync(ex, update, ct);
        }
    }

    private Task HandlePollingError(ITelegramBotClient botClient, Exception exception, CancellationToken ct)
    {
        _logger.LogError(exception, "Ошибка сети или API Telegram (Polling Error)");

        return Task.CompletedTask;
    }
}