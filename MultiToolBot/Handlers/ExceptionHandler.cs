using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace Multitoolbot.Handlers
{
    public class ExceptionHandler
    {
        private readonly ILogger<ExceptionHandler> _logger;
        private readonly ITelegramBotClient _botClient;

        public ExceptionHandler(ILogger<ExceptionHandler> logger, ITelegramBotClient botClient)
        {
            _logger = logger;
            _botClient = botClient;
        }

        public async Task HandleAsync(Exception ex, Update update, CancellationToken ct)
        {
            _logger.LogError(ex, "Ошибка при обработке апдейта {Id}. Тип: {Type}", update.Id, update.Type);

            string message = "";
            message = ex switch
            {
                KeyNotFoundException => ex.Message,
                _ => "Произошла ошибка. Попробуйте позже."
            };

            long? chatId = update.Message?.Chat.Id ?? update.CallbackQuery?.Message?.Chat.Id;
            if (chatId.HasValue)
            {
                await _botClient.SendMessage(chatId.Value, message, cancellationToken: ct);
            }
        }

        
    }
}
