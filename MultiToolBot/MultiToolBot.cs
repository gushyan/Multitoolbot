using Application.Dto;
using FuzzySharp;
using Multitoolbot.Cache;
using PermGorTrans.ApiClient;
using PermGorTrans.ApiClient.Models;
using Services.Interfaces;
using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using static System.Net.Mime.MediaTypeNames;

namespace MultitoolBot
{
    public class MultiToolBot
    {
        private readonly ITelegramBotClient _botClient;
        private readonly IStopPlaceCache _cache;
        private readonly IPermGortransClient _client;
        private readonly IFavStopsService _stopsService;

        public MultiToolBot(ITelegramBotClient botClient, IStopPlaceCache cache, IPermGortransClient client, IFavStopsService stopsService)
        {
            _botClient = botClient;
            _cache = cache;
            _client = client;
            _stopsService = stopsService;
        }

        public async Task HandleMessageAsync(Message message, CancellationToken ct)
        {
            var text = message.Text;
            if (string.IsNullOrEmpty(text)) return;

            var chatId = message.Chat.Id;

            var task = text switch
            {
                "/start" => _botClient.SendMessage(message.Chat.Id, "Привет! Я бот-мультитул...", cancellationToken: ct),
                "/help" => _botClient.SendMessage(message.Chat.Id, "Просто отправь название остановки...", cancellationToken: ct),
                "/favs" => ShowFavsAsync(message.Chat.Id, ct),
                "/addfav" => ShowStopsAsync(text, "choose", message.Chat.Id, ct),
                _ => ShowStopsAsync(text,"stop" ,message.Chat.Id, ct)
            };

            await task;
        }

        public async Task HandleCallbackAsync(CallbackQuery callbackQuery, CancellationToken ct)
        {
            var data = callbackQuery.Data;

            if (data.StartsWith("stop:"))
            {
                var idString = data.Replace("stop:", "");

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

                    string stopName = stop.Name;

                    await _botClient.AnswerCallbackQuery(callbackQuery.Id, $"Загружаю расписание...", cancellationToken: ct);

                    ArrivalResponse arrivalData = await _client.GetArrivalTimesByStops(stopId, ct);

                    string replyText = FormatArrivalMessage(arrivalData, stopName);

                    await _botClient.SendMessage(
                        chatId: callbackQuery.Message.Chat.Id,
                        text: replyText,
                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
                        cancellationToken: ct);
                }
            }
            else if (data.StartsWith("choose:"))
            {
                var idString = data.Replace("choose:", "");
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

                    await _stopsService.AddFavoriteStopsByChatIdAsync(new FavStopsAddRequest(callbackQuery.Message.Chat.Id, stopId), ct);

                    await _botClient.SendMessage(
                        chatId: callbackQuery.Message.Chat.Id,
                        text: $"Остановка {stop.Name} была добавлена в избранное",
                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
                        cancellationToken: ct);
                }
            }
        }

        private async Task ShowStopsAsync(string text, string reason , long chatId, CancellationToken ct) 
        {
            var term = text.ToLower();
            var stopPlaces = _cache.Stops
                .Select(stop => new { Stop = stop, Score = Fuzz.PartialRatio(term, stop.Name.ToLower()) })
                .Where(x => x.Score > 75)
                .OrderByDescending(x => x.Score)
                .Select(x => x.Stop)
                .Take(10)
                .ToList();

            if (stopPlaces.Count == 0)
            {
                await _botClient.SendMessage(chatId, "В базе пока нет такой остановки.", cancellationToken: ct);
                return;
            }

            var buttons = stopPlaces.Select(stop =>
            {
                string cleanNote = stop.Note ?? "";

                if (!string.IsNullOrEmpty(cleanNote) && cleanNote.Contains(stop.Name, StringComparison.OrdinalIgnoreCase))
                {
                    cleanNote = cleanNote.Replace(stop.Name, "", StringComparison.OrdinalIgnoreCase)
                                         .Replace("по ", "", StringComparison.OrdinalIgnoreCase)
                                         .Trim(' ', ',', '(', ')');
                }

                cleanNote = cleanNote.Replace("в город", "➡️ в город")
                                     .Replace("из города", "⬅️ из города");

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

        private string FormatArrivalMessage(ArrivalResponse response, string stopName)
        {
            if (response?.RouteTypes == null || response.RouteTypes.Count == 0)
            {
                return $" На остановке {stopName} в ближайшее время транспорта не ожидается.";
            }

            var sb = new StringBuilder();
            sb.AppendLine($" *Остановка*: {stopName}\n");

            foreach (var type in response.RouteTypes)
            {
                sb.AppendLine($"*{type.RouteTypeName}*");

                foreach (var route in type.Routes)
                {
                    var arrivals = new List<string>();
                    foreach (var vehicle in route.Vehicles)
                    {
                        string timeOnly = vehicle.ArrivalTime.Length >= 5
                            ? vehicle.ArrivalTime.Substring(0, 5)
                            : vehicle.ArrivalTime;

                        string timeStr;
                        if (vehicle.ArrivalMinutes == 0) timeStr = "прибывает";
                        else if (vehicle.ArrivalMinutes < 0) timeStr = "уже ушел";
                        else
                        {
                            var hours = vehicle.ArrivalMinutes / 60;
                            if (hours > 0)
                                timeStr = $" {hours} ч {vehicle.ArrivalMinutes - hours * 60} мин";
                            else
                                timeStr = $"{vehicle.ArrivalMinutes} мин";

                        }

                        arrivals.Add($"{timeStr} ({timeOnly})");
                    }

                    sb.AppendLine($" *{route.RouteNumber}*: {string.Join(", ", arrivals)}");
                }
                sb.AppendLine();
            }

            return sb.ToString().TrimEnd();
        }
    }
}

