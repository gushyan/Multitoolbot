using Application.Dto;
using FuzzySharp;
using Multitoolbot.Cache;
using Multitoolbot.Constants;
using Multitoolbot.Logic;
using PermGorTrans.ApiClient;
using PermGorTrans.ApiClient.Models;
using Services.Interfaces;
using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.ReplyMarkups;

namespace MultitoolBot
{
    public class MultiToolBot
    {
        private readonly ITelegramBotClient _botClient;
        private readonly IStopPlaceCache _cache;
        private readonly IFavStopsService _stopsService;
        private readonly IStopLogic _stopBot;
        private readonly IStopService _stopService;

        public MultiToolBot(ITelegramBotClient botClient, IStopPlaceCache cache, IFavStopsService stopsService, IStopLogic stopBot, IStopService stopService)
        {
            _botClient = botClient;
            _cache = cache;
            _stopsService = stopsService;
            _stopBot = stopBot;
            _stopService = stopService;
        }

        public async Task HandleMessageAsync(Message message, CancellationToken ct)
        {
            var text = message.Text;
            if (string.IsNullOrEmpty(text)) return;

            var chatId = message.Chat.Id;

            var task = text switch
            {
                BotCommands.Start => _botClient.SendMessage(message.Chat.Id, "Привет! Я бот-мультитул...", cancellationToken: ct),
                BotCommands.Help => _botClient.SendMessage(message.Chat.Id, "Просто отправь название остановки...", cancellationToken: ct),
                BotCommands.Favs => _botClient.SendMessage(message.Chat.Id, "В разработке", cancellationToken: ct),
                BotCommands.AddFav => _botClient.SendMessage(message.Chat.Id, "В разработке", cancellationToken: ct),
                _ => ShowStopsAsync(text, "stop", message.Chat.Id, ct)
            };

            await task;
        }

        public async Task HandleCallbackAsync(CallbackQuery callbackQuery, CancellationToken ct)
        {
            var data = callbackQuery.Data;

            if (data.StartsWith(CallbackData.Stop))
            {
                var idString = data.Replace("stop:", "");

                if (int.TryParse(idString, out int stopId)) // дублирование со строкой 87
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
                    }                                       // конец дублирования

                    string stopName = stop.Name;

                    await _botClient.AnswerCallbackQuery(callbackQuery.Id, $"Загружаю расписание...", cancellationToken: ct);

                    ArrivalResponse arrivalData = await _stopService.GetArrivalTimesByStops(stopId, ct);

                    string replyText = _stopBot.FormatArrivalMessage(arrivalData, stopName);

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
                        await _botClient.SendMessage(
                        chatId: callbackQuery.Message.Chat.Id,
                        text: replyText,
                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
                        cancellationToken: ct);
                    }
                }
            }
            //else if (data.StartsWith("choose:")) // пока не реализовано
            //{
            //    var idString = data.Replace("choose:", "");
            //    if (int.TryParse(idString, out int stopId))
            //    {
            //        var stop = _cache.Stops.FirstOrDefault(s => s.Id == stopId);
            //        if (stop == null)
            //        {
            //            await _botClient.AnswerCallbackQuery(
            //                callbackQueryId: callbackQuery.Id,
            //                text: "Эта кнопка устарела. Пожалуйста, воспользуйтесь поиском снова.",
            //                showAlert: false,
            //                cancellationToken: ct);
            //            return;
            //        }

            //        await _stopsService.AddFavoriteStopsByChatIdAsync(new FavStopsAddRequest(callbackQuery.Message.Chat.Id, stopId), ct);

            //        await _botClient.SendMessage(
            //            chatId: callbackQuery.Message.Chat.Id,
            //            text: $"Остановка {stop.Name} была добавлена в избранное",
            //            parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
            //            cancellationToken: ct);
            //    }
            //}
        }

        public async Task HandleInlineQueryAsync(InlineQuery query, CancellationToken ct)
        {
            var stopName = query.Query;
            var stopPlaces = _stopBot.SearchStops(stopName);
            var results = stopPlaces.Select(s =>
                {
                    var messageContent = new InputTextMessageContent($"Расписание для: {s.Name}");
                    var cleanNote = _stopBot.EditNamesStops(s.Note, s.Name);
                    return new InlineQueryResultArticle()
                    {

                        Id = $"inline_stop:{s.Id}",
                        Title = $"{s.Name} ({cleanNote})",
                        InputMessageContent = messageContent,
                        ReplyMarkup = new InlineKeyboardMarkup(
                                        InlineKeyboardButton.WithCallbackData("Узнать время прибытия", $"stop:{s.Id}"))
                    };
                }
                );
            await _botClient.AnswerInlineQuery(
                inlineQueryId: query.Id,
                results: results,
                
                cacheTime:60,
                cancellationToken:ct);
        }

        private async Task ShowStopsAsync(string text, string reason, long chatId, CancellationToken ct)
        {
            var stopPlaces = _stopBot.SearchStops(text);

            if (stopPlaces.Count == 0)
            {
                await _botClient.SendMessage(chatId, "В базе пока нет такой остановки.", cancellationToken: ct);
                return;
            }

            var buttons = stopPlaces.Select(stop =>
            {
                string cleanNote = stop.Note ?? "";
                cleanNote = _stopBot.EditNamesStops(cleanNote, stop.Name);

                string buttonText = string.IsNullOrWhiteSpace(cleanNote)
                    ? stop.Name
                    : $"{stop.Name} ({cleanNote})";

                return InlineKeyboardButton.WithCallbackData(
                    text: buttonText,
                    callbackData: $"{reason}:{stop.Id}"
                );
            });

            var inlineKeyboard = new InlineKeyboardMarkup(buttons.Chunk(1));

            await _botClient.SendMessage(
                chatId: chatId,
                text: "Выберите остановку из списка ниже:",
                replyMarkup: inlineKeyboard,
                cancellationToken: ct
            );
        }

        private async Task ShowFavsAsync(long chatId, CancellationToken ct)
        {
            var favoritStops = await GetFavsAsync(chatId, ct);

            var buttons = favoritStops.Select(s =>
                InlineKeyboardButton.WithCallbackData($"{s.Name} ({s.Note})", $"stop:{s.Id}")
            );

            var inlineKeyboard = new InlineKeyboardMarkup(buttons.Chunk(1));

            await _botClient.SendMessage(
                chatId: chatId,
                text: "Ваши избранные остановки:",
                replyMarkup: inlineKeyboard,
                cancellationToken: ct
            );
        }

        private async Task<List<ExtStopPlace>> GetFavsAsync(long chatId, CancellationToken ct)
        {
            var stops = await _stopsService.GetFavoriteStopsByChatIdAsync(chatId, ct);
            var favoritfaeStops = _cache.Stops.Join(stops.stopIds, o => o.Id, s => s, (cacheStop, favId) => cacheStop);

            return favoritfaeStops.ToList();
        }
    }
}

