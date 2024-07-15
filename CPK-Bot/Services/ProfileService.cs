using CPK_Bot.Data.Context;
using CPK_Bot.Models;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace CPK_Bot.Services;

public class ProfileService
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
            Console.WriteLine($"Error processing user {user.Id}: {ex.Message}");
        }
    }


    public async Task ShowProfileAsync(ITelegramBotClient botClient, long chatId, long userId, BotDbContext dbContext, CancellationToken cancellationToken)
    {
        var profile = await dbContext.Profiles.SingleOrDefaultAsync(p => p.Id == userId, cancellationToken);
        if (profile != null)
        {
            var displayName = string.IsNullOrEmpty(profile.Username) ? profile.FirstName : $"@{profile.Username}";
            await botClient.SendTextMessageAsync(chatId, $"Профиль пользователя {displayName}:\nСоциальный рейтинг: {profile.Rating}\nРоль пользователя: {profile.Role}", cancellationToken: cancellationToken);
        }
        else
        {
            Console.WriteLine($"Profile not found for user ID: {userId}");
            await botClient.SendTextMessageAsync(chatId, "Профиль не найден.", cancellationToken: cancellationToken);
        }
    }

    public async Task ShowProfileByUsernameAsync(ITelegramBotClient botClient, long chatId, string username, BotDbContext dbContext, CancellationToken cancellationToken)
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

    public async Task HandleRateCommandAsync(ITelegramBotClient botClient, Message message, long chatId, BotDbContext dbContext, CancellationToken cancellationToken)
    {
        if (message.From?.Username != "arrogganz")
        {
            await botClient.SendTextMessageAsync(chatId, "У вас нет разрешения на изменение рейтинга.", cancellationToken: cancellationToken);
            return;
        }

        long userId;
        int score;

        if (message.ReplyToMessage != null)
        {
            userId = message.ReplyToMessage.From.Id;
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
            if (parts.Length != 3 || !long.TryParse(parts[1], out userId) || !int.TryParse(parts[2], out score))
            {
                await botClient.SendTextMessageAsync(chatId, "Неверный формат команды. Используйте: /rate [userId] [score] или ответьте на сообщение пользователя командой /rate [score]", cancellationToken: cancellationToken);
                return;
            }
        }

        var profile = await dbContext.Profiles.SingleOrDefaultAsync(p => p.Id == userId, cancellationToken);
        if (profile == null)
        {
            await botClient.SendTextMessageAsync(chatId, "Профиль не найден.", cancellationToken: cancellationToken);
            return;
        }

        profile.Rating += score;
        dbContext.Profiles.Update(profile);
        await dbContext.SaveChangesAsync(cancellationToken);

        await botClient.SendTextMessageAsync(chatId, $"Социальный рейтинг пользователя @{profile.Username} теперь {profile.Rating}.", cancellationToken: cancellationToken);
    }


    public async Task HandleSetRoleCommandAsync(ITelegramBotClient botClient, Message message, long chatId, BotDbContext dbContext, CancellationToken cancellationToken)
    {
        if (message.From?.Username != "arrogganz")
        {
            await botClient.SendTextMessageAsync(chatId, "У вас нет разрешения на изменение ролей.", cancellationToken: cancellationToken);
            return;
        }

        long userId;
        string role;

        if (message.ReplyToMessage != null)
        {
            userId = message.ReplyToMessage.From.Id;
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
            if (parts.Length != 3 || !long.TryParse(parts[1], out userId))
            {
                await botClient.SendTextMessageAsync(chatId, "Неверный формат команды. Используйте: /setrole [userId] [role] или ответьте на сообщение пользователя командой /setrole [role]", cancellationToken: cancellationToken);
                return;
            }

            role = parts[2];
        }

        var profile = await dbContext.Profiles.SingleOrDefaultAsync(p => p.Id == userId, cancellationToken);
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

    public async Task WelcomeNewMembersAsync(ITelegramBotClient botClient, Message message, long chatId, CancellationToken cancellationToken, BotDbContext dbContext)
    {
        foreach (var newMember in message.NewChatMembers!)
        {
            if (newMember.Id != botClient.BotId)
            {
                await RegisterUserAsync(newMember, "Member", dbContext, cancellationToken);
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

    public async Task FarewellMemberAsync(ITelegramBotClient botClient, Message message, long chatId, CancellationToken cancellationToken)
    {
        var leftMember = message.LeftChatMember!;
        if (leftMember.Id != botClient.BotId)
        {
            await botClient.SendTextMessageAsync(
                chatId,
                $"{leftMember.FirstName} покинул(а) чат. Надеемся, что он/она скоро вернется!",
                replyToMessageId: message.MessageId,
                cancellationToken: cancellationToken
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
}
