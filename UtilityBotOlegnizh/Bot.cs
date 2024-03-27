using System;
using System.Collections.Generic;
using System.Text;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.Hosting;
using Telegram.Bot.Types.ReplyMarkups;


namespace UtilityBot
{

    internal class Bot : BackgroundService
    {
        private ITelegramBotClient _telegramClient;

        public Bot(ITelegramBotClient telegramClient)
        {
            _telegramClient = telegramClient;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _telegramClient.StartReceiving(
                HandleUpdateAsync,
                HandleErrorAsync,
                new ReceiverOptions() { AllowedUpdates = { } }, // Здесь выбираем, какие обновления хотим получать. В данном случае разрешены все
                cancellationToken: stoppingToken);

            var me = await _telegramClient.GetMeAsync();
            Console.WriteLine($"{me.FirstName} запущен");

        }

        async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {

            //  Обрабатываем нажатия на кнопки  из Telegram Bot API: https://core.telegram.org/bots/api#callbackquery
            if (update.Type == UpdateType.CallbackQuery)
            {
                await _telegramClient.SendTextMessageAsync(update.CallbackQuery.From.Id, $"Данный тип сообщений не поддерживается. Пожалуйста отправьте текст.", cancellationToken: cancellationToken);
                return;
            }

            // Обрабатываем входящие сообщения из Telegram Bot API: https://core.telegram.org/bots/api#message
            if (update.Type == UpdateType.Message)
            {
                switch (update.Message!.Text)
                {
                    case "/start":
                        Console.WriteLine($"{update.Message.From.FirstName} ({update.Message.From.Id}) написал сообщение: {update.Message.Text}");
                        var buttons = new List<InlineKeyboardButton[]>();
                        buttons.Add(new[]
                        {
                        InlineKeyboardButton.WithCallbackData($" Количество символов в сообщении" , $"simbolsCount"),
                        });
                        buttons.Add(new[]
                        {
                        InlineKeyboardButton.WithCallbackData($" Сумма из цифр в сообщении" , $"sumNumbers")
                        });
                        //await _telegramClient.SendTextMessageAsync(update.Message.From.Id, $"Длина сообщения - {update.Message.Text} - {update.Message.Text.Length} знаков", cancellationToken: cancellationToken);
                        await _telegramClient.SendTextMessageAsync(update.Message.Chat.Id, $"<b>  Бот произведет операцию</b> {Environment.NewLine}" +
                        $"{Environment.NewLine}Выберите с помощью кнопки.{Environment.NewLine}",
                        cancellationToken: cancellationToken, parseMode: ParseMode.Html, replyMarkup: new InlineKeyboardMarkup(buttons));

                        return;

                    default:

                        await _telegramClient.SendTextMessageAsync(update.Message.From.Id, $"Данный тип сообщений не поддерживается. Пожалуйста отправьте текст.", cancellationToken: cancellationToken);
                        return;

                }
            }
        }

        Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            // Задаем сообщение об ошибке в зависимости от того, какая именно ошибка произошла
            var errorMessage = exception switch
            {
                ApiRequestException apiRequestException
                  => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            // Выводим в консоль информацию об ошибке
            Console.WriteLine(errorMessage);

            // Задержка перед повторным подключением
            Console.WriteLine("Ожидаем 10 секунд перед повторным подключением.");
            Thread.Sleep(10000);

            return Task.CompletedTask;
        }
    }

}
