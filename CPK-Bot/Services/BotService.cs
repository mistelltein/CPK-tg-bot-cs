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
            AllowedUpdates = Array.Empty<UpdateType>()
        };

        _bot.StartReceiving(
            HandleUpdateAsync,
            HandleErrorAsync,
            receiverOptions,
            _cts.Token
        );

        using (var scope = _serviceProvider.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<BotDbContext>();
            var cancellationToken = _cts.Token;
            Task.Run(async () => await CleanUpDuplicateProfiles(dbContext, cancellationToken), cancellationToken).Wait();
        }

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
                    "/giveFrontendQuestion\n" + 
                    "/finduser @username\n",
                    cancellationToken: cancellationToken
                );
                break;

            case "/registerAll@it_kyrgyzstan_cs_bot":
            case "/registerAll":
                await RegisterAllMembers(botClient, chatId, dbContext, cancellationToken);
                break;

            case "/profile@it_kyrgyzstan_cs_bot":
            case "/profile":
                await ShowProfile(botClient, chatId, message.From!.Username!, dbContext, cancellationToken);
                break;
            
            case "/backendQuestions@it_kyrgyzstan_cs_bot":
            case "/backendQuestions":
                await botClient.SendTextMessageAsync(chatId, "Ждем реализацию команды", cancellationToken: cancellationToken);
                break;

            case "/giveBackendQuestion@it_kyrgyzstan_cs_bot":
            case "/giveBackendQuestion":
                await botClient.SendTextMessageAsync(chatId, "Ждем реализацию команды", cancellationToken: cancellationToken);
                break;

            case "/frontendQuestions@it_kyrgyzstan_cs_bot":
            case "/frontendQuestions":
                await botClient.SendTextMessageAsync(chatId, "Ждем реализацию команды", cancellationToken: cancellationToken);
                break;

            case "/giveFrontendQuestion@it_kyrgyzstan_cs_bot":
            case "/giveFrontendQuestion":
                await botClient.SendTextMessageAsync(chatId, "Ждем реализацию команды", cancellationToken: cancellationToken);
                break;
            
            case "/cleanup":
                await botClient.SendTextMessageAsync(chatId, "Starting cleanup...", cancellationToken: cancellationToken);
                using (var scope = _serviceProvider.CreateScope())
                {
                    dbContext = scope.ServiceProvider.GetRequiredService<BotDbContext>();
                    await CleanUpDuplicateProfiles(dbContext, cancellationToken);
                }
                await botClient.SendTextMessageAsync(chatId, "Cleanup completed.", cancellationToken: cancellationToken);
                break;
            
            case var command when command.StartsWith("/finduser"):
                var parts = message.Text.Split(' ');
                if (parts.Length == 2)
                {
                    var username = parts[1].TrimStart('@');
                    await ShowProfileByUsername(botClient, chatId, username, dbContext, cancellationToken);
                }
                else
                {
                    await botClient.SendTextMessageAsync(chatId, "Неверный формат команды. Используйте: /finduser @username", cancellationToken: cancellationToken);
                }
                break; 

            default:
                if (message.Text!.StartsWith("/rate"))
                {
                    await HandleRateCommand(botClient, message, chatId, dbContext, cancellationToken);
                }
                else if (message.Text!.StartsWith("/setrole"))
                {
                    await HandleSetRoleCommand(botClient, message, chatId, dbContext, cancellationToken);
                }
                break;
        }
    }

    private async Task HandleRateCommand(ITelegramBotClient botClient, Message message, long chatId, BotDbContext dbContext, CancellationToken cancellationToken)
    {
        if (message.From?.Username != "arrogganz")
        {
            await botClient.SendTextMessageAsync(chatId, "У вас нет разрешения на изменение рейтинга.", cancellationToken: cancellationToken);
            return;
        }

        var parts = message.Text!.Split(' ');
        if (parts.Length != 3)
        {
            await botClient.SendTextMessageAsync(chatId, "Неверный формат команды. Используйте: /rate [username] [score]", cancellationToken: cancellationToken);
            return;
        }

        var username = parts[1].TrimStart('@');
        if (!int.TryParse(parts[2], out var score))
        {
            await botClient.SendTextMessageAsync(chatId, "Неверный формат оценки. Используйте целое число.", cancellationToken: cancellationToken);
            return;
        }

        var profiles = await dbContext.Profiles.Where(p => p.Username == username).ToListAsync(cancellationToken);
        if (profiles.Count == 0)
        {
            await botClient.SendTextMessageAsync(chatId, "Профиль не найден.", cancellationToken: cancellationToken);
            return;
        }

        if (profiles.Count > 1)
        {
            await botClient.SendTextMessageAsync(chatId, "Обнаружено несколько профилей с таким именем пользователя. Пожалуйста, уточните.", cancellationToken: cancellationToken);
            return;
        }

        var profile = profiles.First();
        profile.Rating += score;
        dbContext.Profiles.Update(profile);
        await dbContext.SaveChangesAsync(cancellationToken);

        await botClient.SendTextMessageAsync(chatId, $"Социальный рейтинг пользователя @{profile.Username} теперь {profile.Rating}.", cancellationToken: cancellationToken);
    }
    
    private async Task HandleSetRoleCommand(ITelegramBotClient botClient, Message message, long chatId, BotDbContext dbContext, CancellationToken cancellationToken)
    {
        if (message.From?.Username != "arrogganz")
        {
            await botClient.SendTextMessageAsync(chatId, "У вас нет разрешения на изменение ролей.", cancellationToken: cancellationToken);
            return;
        }

        var parts = message.Text!.Split(' ');
        if (parts.Length != 3)
        {
            await botClient.SendTextMessageAsync(chatId, "Неверный формат команды. Используйте: /setrole [username] [role]", cancellationToken: cancellationToken);
            return;
        }

        var username = parts[1].TrimStart('@');
        var role = parts[2];

        var profile = await dbContext.Profiles.SingleOrDefaultAsync(p => p.Username == username, cancellationToken);
        if (profile == null)
        {
            await botClient.SendTextMessageAsync(chatId, "Профиль не найден.", cancellationToken: cancellationToken);
            return;
        }

        profile.Role = role;
        dbContext.Profiles.Update(profile);
        await dbContext.SaveChangesAsync(cancellationToken);

        await botClient.SendTextMessageAsync(chatId, $"Роль пользователя @{profile.Username} теперь {profile.Role}.", cancellationToken: cancellationToken);
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
            var existingProfile = await dbContext.Profiles.SingleOrDefaultAsync(p => p.Username == user.Username, cancellationToken);
            if (existingProfile == null)
            {
                var profile = new Profile
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
                if (existingProfile.Username != user.Username)
                {
                    existingProfile.Username = user.Username;
                    changed = true;
                }
                if (changed)
                {
                    dbContext.Profiles.Update(existingProfile);
                }
            }
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing user {user.Id}: {ex.Message}");
        }
    }

    private async Task ShowProfile(ITelegramBotClient botClient, long chatId, string username, BotDbContext dbContext, CancellationToken cancellationToken)
    {
        var profile = await dbContext.Profiles.SingleOrDefaultAsync(p => p.Username == username, cancellationToken);
        if (profile != null)
        {
            await botClient.SendTextMessageAsync(chatId, $"Профиль пользователя @{profile.Username}:\nСоциальный рейтинг: {profile.Rating}\nРоль пользователя: {profile.Role}", cancellationToken: cancellationToken);
        }
        else
        {
            Console.WriteLine($"Profile not found for username: {username}");
            await botClient.SendTextMessageAsync(chatId, "Профиль не найден.", cancellationToken: cancellationToken);
        }
    }
    
    private async Task ShowProfileByUsername(ITelegramBotClient botClient, long chatId, string username, BotDbContext dbContext, CancellationToken cancellationToken)
    {
        var profile = await dbContext.Profiles.SingleOrDefaultAsync(p => p.Username == username, cancellationToken);
        if (profile != null)
        {
            await botClient.SendTextMessageAsync(chatId, $"Профиль пользователя @{profile.Username}:\nСоциальный рейтинг: {profile.Rating}\nРоль пользователя: {profile.Role}", cancellationToken: cancellationToken);
        }
        else
        {
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
    
    private async Task CleanUpDuplicateProfiles(BotDbContext dbContext, CancellationToken cancellationToken)
    {
        var duplicates = dbContext.Profiles
            .AsEnumerable()
            .GroupBy(p => p.Username)
            .Where(g => g.Count() > 1)
            .SelectMany(g => g.Skip(1))
            .ToList();

        Console.WriteLine($"Found {duplicates.Count} duplicate profiles");

        foreach (var duplicate in duplicates)
        {
            var mainProfile = await dbContext.Profiles
                .Where(p => p.Username == duplicate.Username)
                .OrderBy(p => p.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (mainProfile != null)
            {
                mainProfile.Rating += duplicate.Rating;
                dbContext.Profiles.Remove(duplicate);
                dbContext.Profiles.Update(mainProfile);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
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