using Application.Constants;
using Application.Dto;
using PermGorTrans.ApiClient.Models;
using Services.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramStopBot.Logic;

namespace TelegramStopBot.Handlers;

public class CallbackHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly IStopsTelegramFormatter _stopsTelegramFormatter;
    private readonly IFavStopsService _favStopsService;
    private readonly IStopService _stopService;

    public CallbackHandler(ITelegramBotClient botClient,
        IStopsTelegramFormatter stopsTelegramFormatter,
        IStopService stopService,
        IFavStopsService favStopsService)
    {
        _botClient = botClient;
        _stopsTelegramFormatter = stopsTelegramFormatter;
        _stopService = stopService;
        _favStopsService = favStopsService;
    }


    public async Task HandleCallbackAsync(CallbackQuery callbackQuery, CancellationToken ct)
    {

        if (callbackQuery.Data?.StartsWith(CallbackData.Stop) == true)
        {
            await HandleShowRoutesRequestAsync(callbackQuery, ct);
        }

        else if (callbackQuery.Data?.StartsWith(CallbackData.Route) == true)
        {
            await HandleShowArrivalTimesByStopsRequestAsync(callbackQuery, ct);
        }

        else if (callbackQuery.Data?.StartsWith(CallbackData.Fav) == true) 
        {
            await AddFavStopsAsync(callbackQuery, ct);
        }
    }

    private async Task HandleShowRoutesRequestAsync(CallbackQuery callbackQuery, CancellationToken ct)
    {
        var data = callbackQuery.Data;
        var idString = data?.Replace(CallbackData.Stop, "");

        if (int.TryParse(idString, out int stopId))
        {
            var choosedRoute = (await _stopService.GetStops(ct)).FirstOrDefault(s => s.Id == stopId);

            if (choosedRoute == null)
            {
                await _botClient.AnswerCallbackQuery(
                   callbackQueryId: callbackQuery.Id,
                   text: "Эта кнопка устарела. Пожалуйста, воспользуйтесь поиском снова.",
                   showAlert: false,
                   cancellationToken: ct);
                return;
            }

            string targetGroupName = choosedRoute.Name.Replace(". ", ".");

            var routes = (await _stopService.GetStops(ct))
                    .Where(s => s.Name.Replace(". ", ".").Equals(targetGroupName, StringComparison.OrdinalIgnoreCase))
                    .ToList();

            var buttons = routes.Select(stop =>
            {
                string buttonText = string.IsNullOrWhiteSpace(stop.Note)
                    ? stop.Name
                    : stop.Note;

                return InlineKeyboardButton.WithCallbackData(
                    text: buttonText,
                    callbackData: $"{CallbackData.Route}{stop.Id}"
                );
            });

            var inlineKeyboard = new InlineKeyboardMarkup(buttons.Chunk(1));

            await _botClient.EditMessageText(
                chatId: callbackQuery.Message!.Chat.Id,
                messageId: callbackQuery.Message.MessageId,
                text: "Выберите направление остановки:",
                replyMarkup: inlineKeyboard,
                cancellationToken: ct
            );
        }
        else
        {
            throw new Exception("Некорректный тип данных");
        }
    }

    private async Task HandleShowArrivalTimesByStopsRequestAsync(CallbackQuery callbackQuery, CancellationToken ct)
    {
        var data = callbackQuery.Data;
        var idString = data.Replace(CallbackData.Route, "");

        ExtStopPlace? stop;
        if (int.TryParse(idString, out int stopId))
        {
            stop = (await _stopService.GetStops(ct)).FirstOrDefault(s => s.Id == stopId);
            if (stop == null)
            {
                await _botClient.AnswerCallbackQuery(
                    callbackQueryId: callbackQuery.Id,
                    text: "Эта кнопка устарела. Пожалуйста, воспользуйтесь поиском снова.",
                    showAlert: false,
                    cancellationToken: ct);
                return;
            }

            await _botClient.AnswerCallbackQuery(callbackQuery.Id, $"Загружаю расписание...", cancellationToken: ct);

            ArrivalResponse arrivalData = await _stopService.GetArrivalTimesByStopsAsync(stopId, ct);

            string replyText = _stopsTelegramFormatter.FormatArrivalMessage(arrivalData, stop.Name, stop.Note);

            if (callbackQuery.InlineMessageId != null)
            {
                await _botClient.EditMessageText(
                    inlineMessageId: callbackQuery.InlineMessageId,
                    text: replyText,
                    parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
                    cancellationToken: ct
                );
            }
            else if (callbackQuery.Message != null)
            {
                InlineKeyboardMarkup? inlineKeyboard;
                if (!await _favStopsService.CheckIsFavouriteStops(new FavExistRequest(callbackQuery.Message.Chat.Id, stopId), ct))
                {
                    var button = InlineKeyboardButton.WithCallbackData(
                    text: $"Добавить остановку {stop.Name} в избранное",

                    callbackData: $"{CallbackData.Fav}{stop.Id}");

                    inlineKeyboard = new InlineKeyboardMarkup(button);
                }
                else
                {
                    inlineKeyboard = null;
                }

                await _botClient.EditMessageText(
                chatId: callbackQuery.Message.Chat.Id,
                messageId: callbackQuery.Message.MessageId,
                text: replyText,
                replyMarkup: inlineKeyboard,
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
                cancellationToken: ct);
            }
        }
        else
        {
            throw new Exception("Некорректный тип данных");
        }

    }

    private async Task AddFavStopsAsync(CallbackQuery callbackQuery, CancellationToken ct)
    {
        var data = callbackQuery.Data;
        var idString = data!.Replace(CallbackData.Fav, "");
        if (int.TryParse(idString, out int stopId))
        {
            await _favStopsService.AddFavoriteStopsByChatIdAsync(new FavStopsAddRequest(callbackQuery.Message!.Chat.Id, stopId), ct);
        }
        else 
        {
            throw new Exception("Некорректный тип данных");
        }

        await _botClient.EditMessageText(
            chatId: callbackQuery.Message.Chat.Id,
            messageId: callbackQuery.Message.MessageId,
            text: callbackQuery.Message.Text + "\n*Добавлено в избранное*",
            parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
            cancellationToken: ct);
    }
}

