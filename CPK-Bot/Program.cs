using Microsoft.Extensions.Configuration;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace CPK_Bot;

internal static class Program
{
    private static TelegramBotClient? _bot;

    private static Task Main()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        var token = configuration["TelegramBotApiKey"];
        if (token != null) _bot = new TelegramBotClient(token);

        var cts = new CancellationTokenSource();

        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = [] // Receive all update types
        };

        _bot?.StartReceiving(
            HandleUpdateAsync,
            HandleErrorAsync,
            receiverOptions,
            cts.Token
        );

        Console.WriteLine("Bot is up and running...");
        Console.ReadLine();
            
        cts.Cancel();
        return Task.CompletedTask;
    }

    private static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update is { Type: UpdateType.Message, Message: not null })
        {
            var message = update.Message;
            var chatId = message.Chat.Id;

            if (message.Text is not null)
            {
                await HandleTextMessageAsync(botClient, message, chatId, cancellationToken);
            }

            await HandleMessageTypeAsync(botClient, message, chatId, cancellationToken);
        }
    }
        
    private static async Task HandleTextMessageAsync(ITelegramBotClient botClient, Message message, long chatId, CancellationToken cancellationToken)
    {
        switch (message.Chat.Type)
        {
            case ChatType.Private when message.Text!.Equals("бот", StringComparison.CurrentCultureIgnoreCase):
                await botClient.SendTextMessageAsync(chatId, "Привет! Как я могу помочь тебе?", cancellationToken: cancellationToken);
                break;
            case ChatType.Supergroup when message.Text!.Equals("бот", StringComparison.CurrentCultureIgnoreCase):
                await botClient.SendTextMessageAsync(chatId, "Чем я могу вам помочь?", cancellationToken: cancellationToken);
                break;
            case ChatType.Group:
                break;
            case ChatType.Channel:
                break;
            case ChatType.Sender:
                break;
            default:
                break;
        }

        switch (message.Text!.ToLower())
        {
            case "/start":
                await botClient.SendTextMessageAsync(
                    chatId,
                    "Привет, вот мои команды:\n" +
                    "/profile" +
                    "/backendQuestions\n" +
                    "/giveBackendQuestion\n" +
                    "/frontendQuestions\n" +
                    "/giveFrontendQuestion",
                    cancellationToken: cancellationToken
                );
                break;
            case "/profile":
                break;
            case "/backendQuestions":
                await botClient.SendTextMessageAsync(
                    chatId,
                    "Ждем реализацию команды",
                    cancellationToken: cancellationToken
                );
                break;
            case "/backendQuestions@it_kyrgyzstan_cs_bot":
                await botClient.SendTextMessageAsync(
                    chatId,
                    "Ждем реализацию команды",
                    cancellationToken: cancellationToken
                );
                break;
            case "/giveBackendQuestion":
                await botClient.SendTextMessageAsync(
                    chatId,
                    "Ждем реализацию команды",
                    cancellationToken: cancellationToken
                );
                break;
            case "/frontendQuestions":
                await botClient.SendTextMessageAsync(
                    chatId,
                    "Ждем реализацию команды",
                    cancellationToken: cancellationToken
                );
                break;
            case "/giveFrontendQuestion":
                await botClient.SendTextMessageAsync(
                    chatId,
                    "Ждем реализацию команды",
                    cancellationToken: cancellationToken
                );
                break;
            default:
                break;
        }
    }

    private static async Task HandleMessageTypeAsync(ITelegramBotClient botClient, Message message, long chatId, CancellationToken cancellationToken)
    {
        switch (message.Type)
        {
            case MessageType.ChatMembersAdded:
                await WelcomeNewMembersAsync(botClient, message, chatId, cancellationToken);
                break;
            case MessageType.ChatMemberLeft when message.LeftChatMember is not null:
                await FarewellMemberAsync(botClient, message, chatId, cancellationToken);
                break;
            default:
                break;
        }
    }
        
    private static async Task WelcomeNewMembersAsync(ITelegramBotClient botClient, Message message, long chatId, CancellationToken cancellationToken)
    {
        foreach (var newMember in message.NewChatMembers!)
        {
            if (newMember.Id != _bot!.BotId)
            {
                await botClient.SendTextMessageAsync(
                    chatId,
                    $"Добро пожаловать, {newMember.FirstName}!\nМожете пожалуйста представиться?\nЕсли есть интересующие вас вопросы, не стесняйтесь их задавать",
                    replyToMessageId: message.MessageId,
                    cancellationToken: cancellationToken
                );
            }
        }
    }

    private static async Task FarewellMemberAsync(ITelegramBotClient botClient, Message message, long chatId, CancellationToken cancellationToken)
    {
        var leftMember = message.LeftChatMember!;
        if (leftMember.Id != _bot!.BotId)
        {
            await botClient.SendTextMessageAsync(
                chatId,
                $"{leftMember.FirstName} покинул(а) чат. Надеемся, что он/она скоро вернется!",
                replyToMessageId: message.MessageId,
                cancellationToken: cancellationToken
            );
        }
    }
        
    private static Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        var errorMessage = exception switch
        {
            ApiRequestException apiRequestException
                => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => exception.ToString()
        };

        Console.WriteLine($"Error occurred: {errorMessage}");
        return Task.CompletedTask;
    }
        
}