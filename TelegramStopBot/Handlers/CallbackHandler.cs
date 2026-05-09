using Application.Constants;
using PermGorTrans.ApiClient.Models;
using Services.Cache;
using Services.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramStopBot.Logic;

namespace TelegramStopBot.Handlers
{
    public class CallbackHandler
    {
        private readonly ITelegramBotClient _botClient;
        private readonly IStopPlaceCache _cache;
        private readonly IStopsTelegramFormatter _stopsTelegramFormatter;
        private readonly IStopService _stopService;

        public CallbackHandler(ITelegramBotClient botClient, IStopPlaceCache cache, IStopsTelegramFormatter stopsTelegramFormatter, IStopService stopService)
        {
            _botClient = botClient;
            _cache = cache;
            _stopsTelegramFormatter = stopsTelegramFormatter;
            _stopService = stopService;
        }


        public async Task HandleCallbackAsync(CallbackQuery callbackQuery, CancellationToken ct)
        {

            if (callbackQuery.Data.StartsWith(CallbackData.Stop) && callbackQuery.Message != null)
            {
                await HandleShowRoutesRequestAsync(callbackQuery, ct);
            }

            if (callbackQuery.Data.StartsWith(CallbackData.Route))
            {
                await HandleShowArrivalTimesByStopsRequestAsync(callbackQuery, ct);
            }
        }

        private async Task HandleShowRoutesRequestAsync(CallbackQuery callbackQuery, CancellationToken ct)
        {
            var data = callbackQuery.Data;
            var idString = data.Replace(CallbackData.Stop, "");

            if (int.TryParse(idString, out int stopId))
            {
                var choosedRoute = _cache.Stops.FirstOrDefault(s => s.Id == stopId);

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

                var routes = _cache.Stops
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
                    chatId: callbackQuery.Message.Chat.Id,
                    messageId: callbackQuery.Message.MessageId,
                    text: "Выберите направление остановки:",
                    replyMarkup: inlineKeyboard,
                    cancellationToken: ct
                );
            }
        }

        private async Task HandleShowArrivalTimesByStopsRequestAsync(CallbackQuery callbackQuery, CancellationToken ct) 
        {
            var data = callbackQuery.Data;
            var idString = data.Replace(CallbackData.Route, "");

            if (int.TryParse(idString, out int stopId))
            {
                var stop = _cache.Stops.FirstOrDefault(s => s.Id == stopId);
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

                ArrivalResponse arrivalData = await _stopService.GetArrivalTimesByStops(stopId, ct);

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
                    await _botClient.EditMessageText(
                    chatId: callbackQuery.Message.Chat.Id,
                    messageId: callbackQuery.Message.MessageId,
                    text: replyText,
                    parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
                    cancellationToken: ct);
                }
            }
        }
    }
}

