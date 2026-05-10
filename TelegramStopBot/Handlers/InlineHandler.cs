using Application.Constants;
using Services.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramStopBot.Handlers;

public class InlineHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly IStopService _stopService;

    public InlineHandler(ITelegramBotClient botClient, IStopService stopService) 
    {
        _botClient = botClient;
        _stopService = stopService;
    }

    public async Task HandleInlineQueryAsync(InlineQuery query, CancellationToken ct)
    {
        var stopName = query.Query;
        if (string.IsNullOrWhiteSpace(stopName) || stopName.Length < 2)
            return;

        var stopPlaces = _stopService.SearchStopsAsync(stopName);
        var results = stopPlaces.Select(s =>
        {
            var messageContent = new InputTextMessageContent($"Расписание для: {s.Name}")
            {
                ParseMode = Telegram.Bot.Types.Enums.ParseMode.Markdown
            };

            return new InlineQueryResultArticle()
            {

                Id = $"{s.Id}",
                Title = s.Name,
                Description = s.Note,
                InputMessageContent = messageContent,
                ReplyMarkup = new InlineKeyboardMarkup(
                                InlineKeyboardButton.WithCallbackData("Узнать время прибытия", $"{CallbackData.Route}{s.Id}"))
            };
        }
            );

        await _botClient.AnswerInlineQuery(
            inlineQueryId: query.Id,
            results: results,
            cacheTime: 60,
            cancellationToken: ct);
    }
}
