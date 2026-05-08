using Multitoolbot.Cache;
using Multitoolbot.Constants;
using Multitoolbot.Logic;
using PermGorTrans.ApiClient.Models;
using Services.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.ReplyMarkups;

namespace MultitoolBot
{
    public class TelegramBot
    {
        private readonly ITelegramBotClient _botClient;
        private readonly IStopPlaceCache _cache;
        private readonly IFavStopsService _favstopsService;
        private readonly IStopsLogic _stopLogic;
        private readonly IStopService _stopService;

        public TelegramBot(ITelegramBotClient botClient, IStopPlaceCache cache, IFavStopsService favStopsService, IStopsLogic stopBot, IStopService stopService)
        {
            _botClient = botClient;
            _cache = cache;
            _favstopsService = favStopsService;
            _stopLogic = stopBot;
            _stopService = stopService;
        }

        public async Task HandleMessageAsync(Message message, CancellationToken ct)
        {
            await _cache.InitializeAsync(ct);

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
                BotCommands.Favs => _botClient.SendMessage(message.Chat.Id, "В разработке", cancellationToken: ct),
                BotCommands.AddFav => _botClient.SendMessage(message.Chat.Id, "В разработке", cancellationToken: ct),
                _ => ShowStopsAsync(text, "stop", message.Chat.Id, ct)
            };

            await task;
        }

        public async Task HandleCallbackAsync(CallbackQuery callbackQuery, CancellationToken ct)
        {
            await _cache.InitializeAsync(ct);
            var data = callbackQuery.Data;

            if (data.StartsWith(CallbackData.Stop) && callbackQuery.Message != null)
            {

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

            if (data.StartsWith(CallbackData.Route))
            {
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

                    string replyText = _stopLogic.FormatArrivalMessage(arrivalData, stop.Name, stop.Note);

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

        public async Task HandleInlineQueryAsync(InlineQuery query, CancellationToken ct)
        {
            await _cache.InitializeAsync(ct);

            var stopName = query.Query;
            if (string.IsNullOrWhiteSpace(stopName) || stopName.Length < 2)
                return;

            var stopPlaces = _stopLogic.SearchStops(stopName);
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

        private async Task ShowStopsAsync(string text, string reason, long chatId, CancellationToken ct)
        {
            var groupedStops = _stopLogic.SearchGroupStops(text);

            if (groupedStops.Count == 0)
            {
                await _botClient.SendMessage(chatId, "В базе пока нет такой остановки.", cancellationToken: ct);
                return;
            }

            var buttons = groupedStops.Select(group => InlineKeyboardButton.WithCallbackData(
                    text: group.Key,
                    callbackData: $"{reason}:{group.First().Id}"));

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
            var stops = await _favstopsService.GetFavoriteStopsByChatIdAsync(chatId, ct);
            var favoritfaeStops = _cache.Stops.Join(stops.stopIds, o => o.Id, s => s, (cacheStop, favId) => cacheStop);

            return favoritfaeStops.ToList();
        }
    }
}

