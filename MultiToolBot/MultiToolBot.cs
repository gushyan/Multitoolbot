using FuzzySharp;
using Multitoolbot.Cache;
using PermGorTrans.ApiClient;
using PermGorTrans.ApiClient.Models;
using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace MultitoolBot
{
    public class MultiToolBot
    {
        private readonly ITelegramBotClient _botClient;
        private readonly IStopPlaceCache _cache;
        private readonly IPermGortransClient _client;

        public MultiToolBot(ITelegramBotClient botClient, IStopPlaceCache cache, IPermGortransClient client)
        {
            _botClient = botClient;
            _cache = cache;
            _client = client;
        }

        public async Task HandleMessageAsync(Message message, CancellationToken ct)
        {
            var text = message.Text;
            var chatId = message.Chat.Id;

            if (text == "/start")
            {
                await _botClient.SendMessage(chatId, "Привет! Я бот, который показывает расписание автобусов на конкретной остановке. " +
                    "Помимо этого в мои обязанности входит посредничество между тобой и Gemini \n" +
                    "А также у меня открытый код https://github.com/gushyan/Multitoolbot");
                return;
            }
            else if (text == "/help")
            {
                await _botClient.SendMessage(chatId, "На данный момент можно получить только расписание автобусов на конкретной остановке." +
                    " Для этого просто отправь часть названия остановки. Об остальном позабочусь я :)");
            }
            else
            {

                var term = text.ToLower();
                var stopPlaces = _cache.Stops
                    .Select(stop => new { Stop = stop, Score = Fuzz.PartialRatio(term, stop.Name.ToLower()) })
                    .Where(x => x.Score > 75)
                    .OrderByDescending(x => x.Score)
                    .Select(x => x.Stop)
                    .Take(10)
                    .ToList();

                if (!stopPlaces.Any())
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
                        callbackData: $"stop:{stop.Id}"
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
                    string stopName = stop != null ? stop.Name : "Неизвестная остановка";

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
        }
        private string FormatArrivalMessage(ArrivalResponse response, string stopName)
        {
            if (response?.RouteTypes == null || response.RouteTypes.Count == 0)
            {
                return $"📭 На остановке **{stopName}** в ближайшее время транспорта не ожидается.";
            }

            var sb = new StringBuilder();
            sb.AppendLine($"🚏 **Остановка:** {stopName}\n");

            foreach (var type in response.RouteTypes)
            {
                string icon = type.RouteTypeName.ToLower().Contains("трамва") ? "🚋" : "🚌";
                sb.AppendLine($"{icon} *{type.RouteTypeName}*");

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
                        else timeStr = $"{vehicle.ArrivalMinutes} мин";

                        arrivals.Add($"{timeStr} ({timeOnly})");
                    }

                    sb.AppendLine($"🔹 **№{route.RouteNumber}**: {string.Join(", ", arrivals)}");
                }
                sb.AppendLine();
            }

            return sb.ToString().TrimEnd();
        }
    }
}

