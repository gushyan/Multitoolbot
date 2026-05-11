using Application.Constants;
using Services.Cache;
using Services.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramStopBot.Handlers;

public class MessageHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly IStopService _stopService;
    private readonly IFavStopsService _favStopsService;

    public MessageHandler(ITelegramBotClient botClient, IStopService stopService, IFavStopsService favStopsService) 
    {
        _botClient = botClient;
        _stopService = stopService;
        _favStopsService = favStopsService;
    }

    public async Task HandleMessageAsync(Message message, CancellationToken ct)
    {
        var text = message.Text;
        if (string.IsNullOrEmpty(text)) return;

        var chatId = message.Chat.Id;

        var task = text switch
        {
            BotCommands.Start => _botClient.SendMessage(message.Chat.Id, "Привет! Я бот, который показывает время прибытия автобусов на любой остановке.\r\n" +
            "Просто отправь мне название (например, ЦУМ), и я пришлю расписание!\r\n" +
            "Кстати, у меня открытый исходный код: https://github.com/gushyan/StopsBot", cancellationToken: ct),
            BotCommands.Help => _botClient.SendMessage(message.Chat.Id, "Отправь название остановки и выбери нужную из списка." +
            "Напиши моё имя и название остановки.\r\n" +
            " Например: @твое_имя_бота ЦУМ\r\n" +
            "Выбери остановку из всплывающего списка, и расписание отправится прямо в текущий чат!", cancellationToken: ct),
            BotCommands.Favs => ShowFavStopsAsync(message.Chat.Id, ct),
            BotCommands.AddFav => _botClient.SendMessage(message.Chat.Id, "В разработке", cancellationToken: ct),
            _ => ShowStopsAsync(text, CallbackData.Stop, message.Chat.Id, ct)
        };

        await task;
    }

    private async Task ShowStopsAsync(string text, string reason, long chatId, CancellationToken ct)
    {
        var groupedStops = await _stopService.SearchGroupStops(text, ct);

        if (groupedStops.Count == 0)
        {
            await _botClient.SendMessage(chatId, "В базе пока нет такой остановки.", cancellationToken: ct);
            return;
        }

        var buttons = groupedStops.Select(group => InlineKeyboardButton.WithCallbackData(
                text: group.Key,
                callbackData: $"{reason}{group.First().Id}"));

        var inlineKeyboard = new InlineKeyboardMarkup(buttons.Chunk(1));

        await _botClient.SendMessage(
            chatId: chatId,
            text: "Выберите остановку из списка ниже:",
            replyMarkup: inlineKeyboard,
            cancellationToken: ct
        );
    }

    private async Task ShowFavStopsAsync(long chatId, CancellationToken ct) 
    {
        var favStops = await _favStopsService.GetFavoriteStopsByChatIdAsync(chatId, ct);
        var allStops = await _stopService.GetStops(ct);

        var stops = favStops.StopIds.Select(si => allStops.FirstOrDefault(s => s.Id == si)).ToList();

        var buttons = stops.Select(stop => InlineKeyboardButton.WithCallbackData(
                text: stop.Name,
                callbackData: $"{CallbackData.Route}{stop.Id}"));

        var inlineKeyboard = new InlineKeyboardMarkup(buttons.Chunk(1));

        await _botClient.SendMessage(
            chatId: chatId,
            text: "Выберите остановку из списка ниже:",
            replyMarkup: inlineKeyboard,
            cancellationToken: ct);
    }
}
