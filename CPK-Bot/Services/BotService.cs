using CPK_Bot.Data.Context;
using CPK_Bot.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace CPK_Bot.Services;

public class BotService
{
    private readonly TelegramBotClient _bot;
    private readonly CancellationTokenSource _cts;
    private readonly IServiceProvider _serviceProvider; 
    
    public BotService(IConfiguration configuration, TelegramBotClient bot, IServiceProvider serviceProvider)
    {
        _bot = bot;
        _cts = new CancellationTokenSource();
        _serviceProvider = serviceProvider;
    }

    public void Start()
    {
        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = [] 
        };

        _bot.StartReceiving(
            HandleUpdateAsync,
            HandleErrorAsync,
            receiverOptions,
            _cts.Token
        );

        Console.WriteLine("Bot is up and running...");
        Console.ReadLine();
        _cts.Cancel();
    }
    
    private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update is { Type: UpdateType.Message, Message: not null })
            {
                var message = update.Message;
                var chatId = message.Chat.Id;

                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<BotDbContext>();

                if (message.Text is not null)
                {
                    await HandleTextMessageAsync(botClient, message, chatId, dbContext, cancellationToken);
                }

                await HandleMessageTypeAsync(botClient, message, chatId, dbContext, cancellationToken);
            }
        }

        private async Task HandleTextMessageAsync(ITelegramBotClient botClient, Message message, long chatId, BotDbContext dbContext, CancellationToken cancellationToken)
        {
            if (message.From != null)
            {
                await RegisterUser(message.From, "Member", dbContext, cancellationToken);
            }
    
            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex)
            {
                Console.WriteLine($"Error saving changes: {ex.Message}");
            }
            
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
                        "/profile\n" +
                        "/backendQuestions\n" +
                        "/giveBackendQuestion\n" +
                        "/frontendQuestions\n" +
                        "/giveFrontendQuestion",
                        cancellationToken: cancellationToken
                    );
                    break;
                
                case "/registerAll@it_kyrgyzstan_cs_bot":
                    await RegisterAllMembers(botClient, chatId, dbContext, cancellationToken);
                    break;
                
                case "/registerAll":
                    await RegisterAllMembers(botClient, chatId, dbContext, cancellationToken);
                    break;
                    
                case "/profile@it_kyrgyzstan_cs_bot":
                    await ShowProfile(botClient, chatId, message.From!.Id, dbContext, cancellationToken);
                    break;
                
                case "/profile":
                    await ShowProfile(botClient, chatId, message.From!.Id, dbContext, cancellationToken);
                    break;
                
                case "/backendQuestions@it_kyrgyzstan_cs_bot":
                    await botClient.SendTextMessageAsync(
                        chatId,
                        "Ждем реализацию команды",
                        cancellationToken: cancellationToken
                    );
                    break;
                
                case "/backendQuestions":
                    await botClient.SendTextMessageAsync(
                        chatId,
                        "Ждем реализацию команды",
                        cancellationToken: cancellationToken
                    );
                    break;
                
                case "/giveBackendQuestion@it_kyrgyzstan_cs_bot":
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
                
                case "/frontendQuestions@it_kyrgyzstan_cs_bot":
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
                
                case "/giveFrontendQuestion@it_kyrgyzstan_cs_bot":
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
        
        private async Task RegisterAllMembers(ITelegramBotClient botClient, long chatId, BotDbContext dbContext, CancellationToken cancellationToken)
        {
            await botClient.SendTextMessageAsync(chatId, "Начинаю регистрацию активных участников чата...", cancellationToken: cancellationToken);

            var registeredCount = 0;
            
            var chat = await botClient.GetChatAsync(chatId, cancellationToken);

            if (chat.Type == ChatType.Private)
            {
                await botClient.SendTextMessageAsync(chatId, "Эта команда работает только в групповых чатах.", cancellationToken: cancellationToken);
                return;
            }
            
            var admins = await botClient.GetChatAdministratorsAsync(chatId, cancellationToken);
            foreach (var admin in admins)
            {
                await RegisterUser(admin.User, "Admin", dbContext, cancellationToken);
                registeredCount++;
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            await botClient.SendTextMessageAsync(chatId, $"Зарегистрировано {registeredCount} активных участников чата.", cancellationToken: cancellationToken);
        }

        private async Task RegisterUser(User user, string role, BotDbContext dbContext, CancellationToken cancellationToken)
        {
            try
            {
                var profile = await dbContext.Profiles.FindAsync(new object[] { user.Id }, cancellationToken);
                if (profile == null)
                {
                    profile = new Profile
                    {
                        Id = user.Id,
                        Username = user.Username,
                        Rating = 0,
                        Role = role
                    };
                    await dbContext.Profiles.AddAsync(profile, cancellationToken);
                }
                else
                {
                    bool changed = false;
                    if (profile.Username != user.Username)
                    {
                        profile.Username = user.Username;
                        changed = true;
                    }
                    if (role != "Member" && profile.Role != role)
                    {
                        profile.Role = role;
                        changed = true;
                    }
                    if (changed)
                    {
                        dbContext.Profiles.Update(profile);
                    }
                }
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing user {user.Id}: {ex.Message}");
            }
        }
        
        private async Task ShowProfile(ITelegramBotClient botClient, long chatId, long userId, BotDbContext dbContext, CancellationToken cancellationToken)
        {
            var profile = await dbContext.Profiles.SingleOrDefaultAsync(p => p.Id == userId, cancellationToken);
            if (profile != null)
            {
                await botClient.SendTextMessageAsync(chatId, $"Профиль пользователя @{profile.Username}:\nСоциальный рейтинг: {profile.Rating}", cancellationToken: cancellationToken);
            }
            else
            {
                Console.WriteLine($"Profile not found for user ID: {userId}");
                await botClient.SendTextMessageAsync(chatId, "Профиль не найден.", cancellationToken: cancellationToken);
            }
        }
        
        private async Task HandleMessageTypeAsync(ITelegramBotClient botClient, Message message, long chatId, BotDbContext dbContext, CancellationToken cancellationToken)
        {
            switch (message.Type)
            {
                case MessageType.ChatMembersAdded:
                    await WelcomeNewMembersAsync(botClient, message, chatId, cancellationToken, dbContext);
                    break;
                case MessageType.ChatMemberLeft when message.LeftChatMember is not null:
                    await FarewellMemberAsync(botClient, message, chatId, cancellationToken);
                    break;
                default:
                    break;
            }
        }

        private async Task WelcomeNewMembersAsync(ITelegramBotClient botClient, Message message, long chatId, CancellationToken cancellationToken, BotDbContext dbContext)
        {
            foreach (var newMember in message.NewChatMembers!)
            {
                if (newMember.Id != _bot.BotId)
                {
                    await RegisterUser(newMember, "Member", dbContext, cancellationToken);
                    await botClient.SendTextMessageAsync(
                        chatId,
                        $"Добро пожаловать, {newMember.FirstName}!\nМожете, пожалуйста, представиться?\nЕсли есть интересующие вас вопросы, не стесняйтесь их задавать",
                        replyToMessageId: message.MessageId,
                        cancellationToken: cancellationToken
                    );
                }
            }
            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex)
            {
                Console.WriteLine($"Error saving changes: {ex.Message}");
            }
        }

        private async Task FarewellMemberAsync(ITelegramBotClient botClient, Message message, long chatId, CancellationToken cancellationToken)
        {
            var leftMember = message.LeftChatMember!;
            if (leftMember.Id != _bot.BotId)
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