using CPK_Bot.Data.Context;
using CPK_Bot.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace CPK_Bot.Services;

public class ProfileService(ILogger<ProfileService> logger)
{
    public async Task RegisterUserAsync(User user, string role, BotDbContext dbContext, CancellationToken cancellationToken)
    {
        try
        {
            var existingProfile = await dbContext.Profiles.SingleOrDefaultAsync(p => p.Id == user.Id, cancellationToken);
            if (existingProfile == null)
            {
                var profile = new Profile
                {
                    Id = user.Id,
                    Username = user.Username,
                    FirstName = user.FirstName,
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
                if (existingProfile.FirstName != user.FirstName)
                {
                    existingProfile.FirstName = user.FirstName;
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
            logger.LogError($"Error processing user {user.Id}: {ex.Message}");
        }
    }

    public async Task ShowProfileAsync(ITelegramBotClient botClient, long chatId, long userId, BotDbContext dbContext, CancellationToken cancellationToken)
    {
        try
        {
            var profile = await dbContext.Profiles.SingleOrDefaultAsync(p => p.Id == userId, cancellationToken);
            if (profile != null)
            {
                var displayName = !string.IsNullOrEmpty(profile.Username) ? $"@{profile.Username}" : profile.FirstName ?? "NoName";
                await SendMessageIfNotBlockedAsync(botClient, chatId, $"Профиль пользователя {displayName}:\nСоциальный рейтинг: {profile.Rating}\nРоль пользователя: {profile.Role}", cancellationToken);
            }
            else
            {
                logger.LogWarning($"Profile not found for user ID: {userId}");
                await SendMessageIfNotBlockedAsync(botClient, chatId, "Профиль не найден.", cancellationToken);
            }
        }
        catch (DbUpdateException dbEx)
        {
            logger.LogError($"Database error: {dbEx.Message}");
            await SendMessageIfNotBlockedAsync(botClient, chatId, "Произошла ошибка базы данных. Пожалуйста, повторите попытку позже.", cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError($"Error showing profile for user ID: {userId}. Exception: {ex.Message}");
            await SendMessageIfNotBlockedAsync(botClient, chatId, "Произошла ошибка при попытке отображения профиля.", cancellationToken);
        }
    }

    public async Task ShowProfileByUsernameAsync(ITelegramBotClient botClient, long chatId, string username, BotDbContext dbContext, CancellationToken cancellationToken)
    {
        var profile = await dbContext.Profiles.FirstOrDefaultAsync(p => p.Username == username, cancellationToken);
        if (profile != null)
        {
            await SendMessageIfNotBlockedAsync(botClient, chatId, $"Профиль пользователя @{profile.Username}:\nСоциальный рейтинг: {profile.Rating}\nРоль пользователя: {profile.Role}", cancellationToken);
        }
        else
        {
            await SendMessageIfNotBlockedAsync(botClient, chatId, "Профиль не найден.", cancellationToken: cancellationToken);
        }
    }

    public async Task HandleRateCommandAsync(ITelegramBotClient botClient, Message message, long chatId, BotDbContext dbContext, CancellationToken cancellationToken)
    {
        if (message.From?.Username != "arrogganz")
        {
            await SendMessageIfNotBlockedAsync(botClient, chatId, "У вас нет разрешения на изменение рейтинга.", cancellationToken: cancellationToken);
            return;
        }

        string username;
        int score;

        if (message.ReplyToMessage != null)
        {
            username = message.ReplyToMessage.From!.Username!;
            var parts = message.Text!.Split(' ');

            if (parts.Length != 2 || !int.TryParse(parts[1], out score))
            {
                await SendMessageIfNotBlockedAsync(botClient, chatId, "Неверный формат команды. Используйте: /rate [score]", cancellationToken: cancellationToken);
                return;
            }
        }
        else
        {
            var parts = message.Text!.Split(' ');
            if (parts.Length != 3 || string.IsNullOrEmpty(parts[1]) || !int.TryParse(parts[2], out score))
            {
                await SendMessageIfNotBlockedAsync(botClient, chatId, "Неверный формат команды. Используйте: /rate @username [score] или ответьте на сообщение пользователя командой /rate [score]", cancellationToken: cancellationToken);
                return;
            }
            username = parts[1].TrimStart('@');
        }

        var profile = await dbContext.Profiles.SingleOrDefaultAsync(p => p.Username == username, cancellationToken);
        if (profile == null)
        {
            await SendMessageIfNotBlockedAsync(botClient, chatId, "Профиль не найден.", cancellationToken: cancellationToken);
            return;
        }

        profile.Rating += score;
        dbContext.Profiles.Update(profile);
        await dbContext.SaveChangesAsync(cancellationToken);

        await SendMessageIfNotBlockedAsync(botClient, chatId, $"Социальный рейтинг пользователя @{profile.Username} теперь {profile.Rating}.", cancellationToken: cancellationToken);
    }

    public async Task HandleSetRoleCommandAsync(ITelegramBotClient botClient, Message message, long chatId, BotDbContext dbContext, CancellationToken cancellationToken)
    {
        if (message.From?.Username != "arrogganz")
        {
            await SendMessageIfNotBlockedAsync(botClient, chatId, "У вас нет разрешения на изменение ролей.", cancellationToken: cancellationToken);
            return;
        }

        string username;
        string role;

        if (message.ReplyToMessage != null)
        {
            username = message.ReplyToMessage.From!.Username!;
            var parts = message.Text!.Split(' ');

            if (parts.Length != 2)
            {
                await SendMessageIfNotBlockedAsync(botClient, chatId, "Неверный формат команды. Используйте: /setrole [role]", cancellationToken: cancellationToken);
                return;
            }

            role = parts[1];
        }
        else
        {
            var parts = message.Text!.Split(' ');
            if (parts.Length != 3 || string.IsNullOrEmpty(parts[1]))
            {
                await SendMessageIfNotBlockedAsync(botClient, chatId, "Неверный формат команды. Используйте: /setrole @username [role] или ответьте на сообщение пользователя командой /setrole [role]", cancellationToken: cancellationToken);
                return;
            }

            username = parts[1].TrimStart('@');
            role = parts[2];
        }

        var profile = await dbContext.Profiles.SingleOrDefaultAsync(p => p.Username == username, cancellationToken);
        if (profile == null)
        {
            await SendMessageIfNotBlockedAsync(botClient, chatId, "Профиль не найден.", cancellationToken: cancellationToken);
            return;
        }

        profile.Role = role;
        dbContext.Profiles.Update(profile);
        await dbContext.SaveChangesAsync(cancellationToken);

        await SendMessageIfNotBlockedAsync(botClient, chatId, $"Роль пользователя @{profile.Username} теперь {profile.Role}.", cancellationToken: cancellationToken);
    }

    public async Task HandleBanCommandAsync(ITelegramBotClient botClient, BotDbContext dbContext, Message message, long chatId, CancellationToken cancellationToken)
    {
        if (message.From?.Username != "arrogganz")
        {
            await SendMessageIfNotBlockedAsync(botClient, chatId, "У вас нет разрешения на бан пользователей.", cancellationToken: cancellationToken);
            return;
        }

        string username;

        if (message.ReplyToMessage != null)
        {
            username = message.ReplyToMessage.From!.Username!;
        }
        else
        {
            var parts = message.Text!.Split(' ');
            if (parts.Length != 2 || string.IsNullOrEmpty(parts[1]))
            {
                await SendMessageIfNotBlockedAsync(botClient, chatId, "Неверный формат команды. Используйте: /ban @username или ответьте на сообщение пользователя командой /ban", cancellationToken: cancellationToken);
                return;
            }
            username = parts[1].TrimStart('@');
        }

        var profile = await dbContext.Profiles.SingleOrDefaultAsync(p => p.Username == username, cancellationToken);
        if (profile == null)
        {
            await SendMessageIfNotBlockedAsync(botClient, chatId, "Профиль не найден.", cancellationToken: cancellationToken);
            return;
        }

        await botClient.BanChatMemberAsync(chatId, profile.Id, cancellationToken: cancellationToken);
        await SendMessageIfNotBlockedAsync(botClient, chatId, $"Пользователь @{profile.Username} был забанен.", cancellationToken: cancellationToken);
    }

    public async Task WelcomeNewMembersAsync(ITelegramBotClient botClient, Message message, long chatId, CancellationToken cancellationToken, BotDbContext dbContext)
    {
        foreach (var newMember in message.NewChatMembers!)
        {
            if (newMember.Id != botClient.BotId)
            {
                await RegisterUserAsync(newMember, "Member", dbContext, cancellationToken);
                await SendMessageIfNotBlockedAsync(
                    botClient,
                    chatId,
                    $"Добро пожаловать, {newMember.Username}!\nМожете, пожалуйста, представиться?\nЕсли есть интересующие вас вопросы, не стесняйтесь их задавать",
                    cancellationToken
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

    public async Task FarewellMemberAsync(ITelegramBotClient botClient, Message message, long chatId, CancellationToken cancellationToken)
    {
        var leftMember = message.LeftChatMember!;
        if (leftMember.Id != botClient.BotId)
        {
            await SendMessageIfNotBlockedAsync(
                botClient,
                chatId,
                $"{leftMember.Username} покинул(а) чат. Надеемся, что он/она скоро вернется!",
                cancellationToken
            );
        }
    }

    public async Task CleanUpDuplicateProfilesAsync(BotDbContext dbContext, CancellationToken cancellationToken)
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

    private async Task SendMessageIfNotBlockedAsync(ITelegramBotClient botClient, long chatId, string text, CancellationToken cancellationToken)
    {
        try
        {
            var chatMember = await botClient.GetChatMemberAsync(chatId, (long)botClient.BotId!, cancellationToken);
            if (chatMember.Status != ChatMemberStatus.Kicked && chatMember.Status != ChatMemberStatus.Left)
            {
                await botClient.SendTextMessageAsync(chatId, text, cancellationToken: cancellationToken);
            }
        }
        catch (ApiRequestException ex) when (ex.ErrorCode == 403)
        {
            logger.LogWarning($"Bot was blocked by the user with chatId: {chatId}");
        }
        catch (Exception ex)
        {
            logger.LogError($"An error occurred while sending message to chatId: {chatId}. Exception: {ex.Message}");
        }
    }
}