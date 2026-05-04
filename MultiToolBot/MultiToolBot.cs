using FuzzySharp;
using Multitoolbot.Cache;
using Services.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace MultitoolBot
{
    public class MultiToolBot
    {
        private readonly ITelegramBotClient _botClient;
        private readonly IStopPlaceCache _cache;

        public MultiToolBot(ITelegramBotClient botClient, IStopPlaceCache cache)
        {
            _botClient = botClient;
            _cache = cache;
        }

        public async Task HandleMessageAsync(Message message, CancellationToken ct)
        {
            var text = message.Text;
            var chatId = message.Chat.Id;

            if (text == "/start")
            {
                await _botClient.SendMessage(chatId, "Привет! Я бот, который показывает расписание автобусов на конкретной остановки. " +
                    "Помимо этого в мои обязанности входит посредничество между тобой и Gemini \n" +
                    "А также у меня открытый код https://github.com/gushyan/Multitoolbot");
                return;
            }

            else
            {

                var term = text.ToLower();
                var stopPlaces = _cache.Stops
                    .Select(stop => new { Stop = stop, Score = Fuzz.PartialRatio(term, stop.Name.ToLower()) })
                    .Where(x => x.Score > 75) // Высокий порог, чтобы не искать остановки в обычных словах
                    .OrderByDescending(x => x.Score)
                    .Select(x => x.Stop)
                    .ToList();

                //var stopPlaces = _cache.Stops.Take(5).ToList();

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

            if (data.StartsWith("select_stop:"))
            {
                var stopName = data.Replace("select_stop:", "");

                await _botClient.AnswerCallbackQuery(callbackQuery.Id, $"Вы выбрали {stopName}", cancellationToken: ct);

                await _botClient.SendMessage(callbackQuery.Message.Chat.Id, $"Расписание для остановки {stopName}: ...");
            }
        }
    }
}
