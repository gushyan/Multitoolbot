using Application.Constants;
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
            "Кстати, у меня открытый исходный код: https://github.com/gushyan/StopsBot", cancellationToken: ct),
            BotCommands.Help => _botClient.SendMessage(message.Chat.Id, "1. Чтобы получить расписание в боте, отправь название остановки и выбери нужную из списка.\r\n" +
            "2. Чтобы получить расписание в любом другом чате, напиши моё имя и название остановки.\r\n" +
            "Например: @NeMultitool_bot ЦУМ\r\n" +
            "Выбери остановку из всплывающего списка, и расписание отправится прямо в текущий чат!\r\n" +
            "3. Также можно добавить остановку в избранное. Для этого найди остановку через поиск в боте, а затем нажми кнопку \"Добавить (название остановки) в избранное\". \r\n" +
            $"4. Для того чтобы получить избранные остановки, нужно использовать команду {BotCommands.Favs}.\r\n" + 
            $"5. Чтобы удалить остановку из избранного, используй команду {BotCommands.DeleteFav} и выбери нужную остановку.", cancellationToken: ct),
            BotCommands.Favs => ShowFavStopsAsync(message.Chat.Id, CallbackData.ShowRoute, ct),
            BotCommands.DeleteFav => ShowFavStopsAsync(message.Chat.Id, CallbackData.DeleteFav, ct),
            BotCommands.Contact => _botClient.SendMessage(message.Chat.Id, "По всем вопросам обращайтесь к @zhong_ly."),
            _ => ShowStopsAsync(text, CallbackData.ShowStop, message.Chat.Id, ct)
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

        var buttons = groupedStops
            .Select(group =>
                InlineKeyboardButton.WithCallbackData(
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

    private async Task ShowFavStopsAsync(long chatId,string reason, CancellationToken ct)
    {
        var favStops = await _favStopsService.GetFavoriteStopsByChatIdAsync(chatId, ct);
        var allStops = await _stopService.GetStops(ct);

        var stops = favStops.StopIds.Select(si =>
            allStops
                .FirstOrDefault(s => s.Id == si))
                .Where(s => s != null)
                .ToList();

        var buttons = stops
            .Select(stop => 
                InlineKeyboardButton.WithCallbackData(
                    text: stop!.Name,
                    callbackData: $"{reason}{stop.Id}"));

        var inlineKeyboard = new InlineKeyboardMarkup(buttons.Chunk(1));

        string textReason ="";
        if (reason == CallbackData.DeleteFav)
            textReason = "удаления";
        else if (reason == CallbackData.ShowStop)
            textReason = "просмотра";
            
        await _botClient.SendMessage(
            chatId: chatId,
            text: $"Выберите остановку из списка ниже для {textReason}:",
            replyMarkup: inlineKeyboard,
            cancellationToken: ct);
    }
}
