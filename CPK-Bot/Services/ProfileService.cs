using CPK_Bot.Data.Context;
using CPK_Bot.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace CPK_Bot.Services;

public class ProfileService
{
    private readonly ILogger<ProfileService> _logger;

    public ProfileService(ILogger<ProfileService> logger)
    {
        _logger = logger;
    }

    public async Task RegisterUserAsync(User user, string role, BotDbContext dbContext, CancellationToken cancellationToken)
    {
        try
        {
            var existingProfiles = await dbContext.Profiles.Where(p => p.Id == user.Id).ToListAsync(cancellationToken);
            var existingProfile = existingProfiles.FirstOrDefault();

            if (existingProfile == null)
            {
                var profile = new Profile
                {
                    Id = user.Id,
                    Username = user.Username,
                    FirstName = user.FirstName,
                    Rating = 30,
                    Role = role
                };
                await dbContext.Profiles.AddAsync(profile, cancellationToken);
            }
            else
            {
                var changed = false;
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

                foreach (var duplicate in existingProfiles.Skip(1))
                {
                    dbContext.Profiles.Remove(duplicate);
                }
            }
            await dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation($"User {user.Username} registered/updated successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error processing user {user.Id}: {ex.Message}");
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
                await botClient.SendTextMessageAsync(chatId, $"Профиль пользователя {displayName}:\nСоциальный рейтинг: {profile.Rating}\nРоль пользователя: {profile.Role}", cancellationToken: cancellationToken);
                _logger.LogInformation($"Profile of user {displayName} displayed successfully.");
            }
            else
            {
                _logger.LogWarning($"Profile not found for user ID: {userId}");
                await botClient.SendTextMessageAsync(chatId, "Профиль не найден.", cancellationToken: cancellationToken);
            }
        }
        catch (DbUpdateException dbEx)
        {
            _logger.LogError($"Database error: {dbEx.Message}");
            await botClient.SendTextMessageAsync(chatId, "Произошла ошибка базы данных. Пожалуйста, повторите попытку позже.", cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error showing profile for user ID: {userId}. Exception: {ex.Message}");
            await botClient.SendTextMessageAsync(chatId, "Произошла ошибка при попытке отображения профиля.", cancellationToken: cancellationToken);
        }
    }

    public async Task ShowProfileByUsernameAsync(ITelegramBotClient botClient, long chatId, string username, BotDbContext dbContext, CancellationToken cancellationToken)
    {
        var profile = await FindProfileByUsernameAsync(username, dbContext, cancellationToken);
        if (profile != null)
        {
            await botClient.SendTextMessageAsync(chatId, $"Профиль пользователя @{profile.Username}:\nСоциальный рейтинг: {profile.Rating}\nРоль пользователя: {profile.Role}", cancellationToken: cancellationToken);
            _logger.LogInformation($"Profile of user @{profile.Username} displayed successfully.");
        }
        else
        {
            _logger.LogWarning($"Profile not found for username: {username}");
            await botClient.SendTextMessageAsync(chatId, "Профиль не найден.", cancellationToken: cancellationToken);
        }
    }

    public async Task HandleRateCommandAsync(ITelegramBotClient botClient, Message message, long chatId, BotDbContext dbContext, CancellationToken cancellationToken)
    {
        if (message.From?.Username != "arrogganz")
        {
            await botClient.SendTextMessageAsync(chatId, "У вас нет разрешения на изменение рейтинга.", cancellationToken: cancellationToken);
            return;
        }

        string? username;
        int score;

        if (message.ReplyToMessage != null)
        {
            username = message.ReplyToMessage.From!.Username;
            var parts = message.Text!.Split(' ');

            if (parts.Length != 2 || !int.TryParse(parts[1], out score))
            {
                await botClient.SendTextMessageAsync(chatId, "Неверный формат команды. Используйте: /rate [score]", cancellationToken: cancellationToken);
                return;
            }
        }
        else
        {
            var parts = message.Text!.Split(' ');
            if (parts.Length != 3 || string.IsNullOrEmpty(parts[1]) || !int.TryParse(parts[2], out score))
            {
                await botClient.SendTextMessageAsync(chatId, "Неверный формат команды. Используйте: /rate [username] [score] или ответьте на сообщение пользователя командой /rate [score]", cancellationToken: cancellationToken);
                return;
            }

            username = parts[1].TrimStart('@');
        }

        var profile = await FindProfileByUsernameAsync(username!, dbContext, cancellationToken);
        if (profile == null)
        {
            await botClient.SendTextMessageAsync(chatId, "Профиль не найден.", cancellationToken: cancellationToken);
            return;
        }

        profile.Rating += score;
        dbContext.Profiles.Update(profile);
        await dbContext.SaveChangesAsync(cancellationToken);

        await botClient.SendTextMessageAsync(chatId, $"Социальный рейтинг пользователя @{profile.Username} теперь {profile.Rating}.", cancellationToken: cancellationToken);
        _logger.LogInformation($"Rating of user @{profile.Username} updated to {profile.Rating}.");
    }

    public async Task HandleSetRoleCommandAsync(ITelegramBotClient botClient, Message message, long chatId, BotDbContext dbContext, CancellationToken cancellationToken)
    {
        if (message.From?.Username != "arrogganz")
        {
            await botClient.SendTextMessageAsync(chatId, "У вас нет разрешения на изменение ролей.", cancellationToken: cancellationToken);
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
                await botClient.SendTextMessageAsync(chatId, "Неверный формат команды. Используйте: /setrole [role]", cancellationToken: cancellationToken);
                return;
            }

            role = parts[1];
        }
        else
        {
            var parts = message.Text!.Split(' ');
            if (parts.Length != 3 || string.IsNullOrEmpty(parts[1]))
            {
                await botClient.SendTextMessageAsync(chatId, "Неверный формат команды. Используйте: /setrole [username] [role] или ответьте на сообщение пользователя командой /setrole [role]", cancellationToken: cancellationToken);
                return;
            }

            username = parts[1].TrimStart('@');
            role = parts[2];
        }

        var profile = await FindProfileByUsernameAsync(username, dbContext, cancellationToken);
        if (profile == null)
        {
            await botClient.SendTextMessageAsync(chatId, "Профиль не найден.", cancellationToken: cancellationToken);
            return;
        }

        profile.Role = role;
        dbContext.Profiles.Update(profile);
        await dbContext.SaveChangesAsync(cancellationToken);

        await botClient.SendTextMessageAsync(chatId, $"Роль пользователя @{profile.Username} теперь {profile.Role}.", cancellationToken: cancellationToken);
        _logger.LogInformation($"Role of user @{profile.Username} updated to {profile.Role}.");
    }

    public async Task HandleBanCommandAsync(ITelegramBotClient botClient, Message message, long chatId, BotDbContext dbContext, CancellationToken cancellationToken)
    {
        if (message.From?.Username != "arrogganz")
        {
            await botClient.SendTextMessageAsync(chatId, "У вас нет разрешения на бан пользователей.", cancellationToken: cancellationToken);
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
                await botClient.SendTextMessageAsync(chatId, "Неверный формат команды. Используйте: /ban [username] или ответьте на сообщение пользователя командой /ban", cancellationToken: cancellationToken);
                return;
            }

            username = parts[1].TrimStart('@');
        }

        var profile = await FindProfileByUsernameAsync(username, dbContext, cancellationToken);
        if (profile == null)
        {
            await botClient.SendTextMessageAsync(chatId, "Профиль не найден.", cancellationToken: cancellationToken);
            return;
        }

        await botClient.BanChatMemberAsync(chatId, profile.Id, cancellationToken: cancellationToken);
        await botClient.SendTextMessageAsync(chatId, $"Пользователь @{profile.Username} был забанен.", cancellationToken: cancellationToken);
        _logger.LogInformation($"User @{profile.Username} was banned.");
    }

    public async Task WelcomeNewMembersAsync(ITelegramBotClient botClient, Message message, long chatId, CancellationToken cancellationToken, BotDbContext dbContext)
    {
        var tasks = message.NewChatMembers!
            .Where(newMember => newMember.Id != botClient.BotId)
            .Select(async newMember =>
            {
                await RegisterUserAsync(newMember, "Newbie-Developer", dbContext, cancellationToken);
                await botClient.SendTextMessageAsync(
                    chatId,
                    $"Добро пожаловать, {newMember.Username}!\nМожете, пожалуйста, представиться?\nЕсли есть интересующие вас вопросы, не стесняйтесь их задавать",
                    replyToMessageId: message.MessageId,
                    cancellationToken: cancellationToken
                );
            }).ToList();

        try
        {
            await Task.WhenAll(tasks);
            await dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("New members welcomed successfully.");
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError($"Error saving changes: {ex.Message}");
        }
    }

    public async Task FarewellMemberAsync(ITelegramBotClient botClient, Message message, long chatId, CancellationToken cancellationToken)
    {
        var leftMember = message.LeftChatMember!;
        if (leftMember.Id != botClient.BotId)
        {
            await botClient.SendTextMessageAsync(
                chatId,
                $"{leftMember.Username} покинул(а) чат. Надеемся, что он/она скоро вернется!",
                replyToMessageId: message.MessageId,
                cancellationToken: cancellationToken
            );
            _logger.LogInformation($"User {leftMember.Username} left the chat.");
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

        _logger.LogInformation($"Found {duplicates.Count} duplicate profiles");

        foreach (var duplicate in duplicates)
        {
            var mainProfile = await dbContext.Profiles
                .Where(p => p.Username == duplicate.Username)
                .OrderBy(p => p.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (mainProfile == null) continue;
            mainProfile.Rating += duplicate.Rating;
            dbContext.Profiles.Remove(duplicate);
            dbContext.Profiles.Update(mainProfile);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Duplicate profiles cleaned up successfully.");
    }

    private async Task<Profile?> FindProfileByUsernameAsync(string username, BotDbContext dbContext, CancellationToken cancellationToken)
    {
        return await dbContext.Profiles.FirstOrDefaultAsync(p => p.Username == username, cancellationToken);
    }
    
    public async Task<List<Profile>> GetProfilesByRoleAsync(string role, BotDbContext dbContext, CancellationToken cancellationToken)
    {
        var normalizedRole = role.ToLower(); 
        return await dbContext.Profiles
            .Where(p => p.Role!.ToLower() == normalizedRole) 
            .ToListAsync(cancellationToken);
    }
}