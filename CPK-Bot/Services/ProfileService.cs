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
            var existingProfile = await dbContext.Profiles
                .SingleOrDefaultAsync(p => p.Id == user.Id, cancellationToken);

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
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("User {Username} registered/updated successfully.", user.Username);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error processing user {UserId}: {ErrorMessage}", user.Id, ex.Message);
            throw;
        }
    }

    public async Task ShowProfileAsync(ITelegramBotClient botClient, long chatId, long userId, 
        BotDbContext dbContext, CancellationToken cancellationToken)
    {
        try
        {
            var profile = await dbContext.Profiles.SingleOrDefaultAsync(p => p.Id == userId, cancellationToken);
            if (profile != null)
            {
                var displayName = !string.IsNullOrEmpty(profile.Username) ? $"@{profile.Username}" : profile.FirstName ?? "NoName";
                await botClient.SendTextMessageAsync(chatId, $"User profile {displayName}:\n" + 
                                $"Social rating: {profile.Rating}\nUser role: {profile.Role}", cancellationToken: cancellationToken);
                _logger.LogInformation("Profile of user {DisplayName} displayed successfully.", displayName);
            }
            else
            {
                _logger.LogWarning("Profile not found for user ID: {UserId}", userId);
                await botClient.SendTextMessageAsync(chatId, "Profile not found.", cancellationToken: cancellationToken);
            }
        }
        catch (DbUpdateException dbEx)
        {
            _logger.LogError("Database error: {DatabaseUpdateError}", dbEx.Message);
            await botClient.SendTextMessageAsync(chatId, "A database error occurred. Please try again later.", cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error showing profile for user ID: {UserId}. Exception: {ErrorMessage}", userId, ex.Message);
            await botClient.SendTextMessageAsync(chatId, "An error occurred while attempting to display the profile.", cancellationToken: cancellationToken);
        }
    }

    public async Task ShowProfileByUsernameAsync(ITelegramBotClient botClient, long chatId, string username, 
        BotDbContext dbContext, CancellationToken cancellationToken)
    {
        var profile = await FindProfileByUsernameAsync(username, dbContext, cancellationToken);
        if (profile != null)
        {
            await botClient.SendTextMessageAsync(chatId, $"User profile @{profile.Username}:\n" +
                            $"Social rating: {profile.Rating}\nUser role: {profile.Role}", cancellationToken: cancellationToken);
            _logger.LogInformation("Profile of user @{Username} displayed successfully.", profile.Username);
        }
        else
        {
            _logger.LogWarning("Profile not found for username: {Username}", username);
            await botClient.SendTextMessageAsync(chatId, "Profile not found.", cancellationToken: cancellationToken);
        }
    }

    public async Task RateCommandAsync(ITelegramBotClient botClient, Message message, long chatId, 
        BotDbContext dbContext, CancellationToken cancellationToken)
    {
        if (message.From?.Username != "arrogganz")
        {
            await botClient.SendTextMessageAsync(chatId, "You do not have permission to change the rating.", cancellationToken: cancellationToken);
            return;
        }

        long userId;
        int score;

        if (message.ReplyToMessage != null)
        {
            userId = message.ReplyToMessage.From!.Id;
            var parts = message.Text!.Split(' ');

            if (parts.Length != 2 || !int.TryParse(parts[1], out score))
            {
                await botClient.SendTextMessageAsync(chatId, "Invalid command format. Use: /rate [score]", cancellationToken: cancellationToken);
                return;
            }
        }
        else
        {
            var parts = message.Text!.Split(' ');
            if (parts.Length != 3 || string.IsNullOrEmpty(parts[1]) || !int.TryParse(parts[2], out score))
            {
                await botClient.SendTextMessageAsync(chatId, "Invalid command format. Use: /rate [username] [score] " +
                                "or reply to the user's message with the command /rate [score]", cancellationToken: cancellationToken);
                return;
            }

            var username = parts[1].TrimStart('@');
            var profile = await FindProfileByUsernameAsync(username, dbContext, cancellationToken);
            if (profile == null)
            {
                await botClient.SendTextMessageAsync(chatId, "Profile not found.", cancellationToken: cancellationToken);
                return;
            }

            userId = profile.Id;
        }

        var userProfile = await dbContext.Profiles.SingleOrDefaultAsync(p => p.Id == userId, cancellationToken);
        if (userProfile == null)
        {
            await botClient.SendTextMessageAsync(chatId, "Profile not found.", cancellationToken: cancellationToken);
            return;
        }

        userProfile.Rating += score;
        dbContext.Profiles.Update(userProfile);
        await dbContext.SaveChangesAsync(cancellationToken);

        var displayName = !string.IsNullOrEmpty(userProfile.Username) ? $"@{userProfile.Username}" : userProfile.FirstName ?? "NoName";
        await botClient.SendTextMessageAsync(chatId, $"Social rating of user {displayName} is now {userProfile.Rating}.", cancellationToken: cancellationToken);
        _logger.LogInformation("Rating of user {DisplayName} updated to {UserRating}.", displayName, userProfile.Rating);
    }

    public async Task SetRoleCommandAsync(ITelegramBotClient botClient, Message message, long chatId, 
        BotDbContext dbContext, CancellationToken cancellationToken)
    {
        if (message.From?.Username != "arrogganz")
        {
            await botClient.SendTextMessageAsync(chatId, "You do not have permission to change roles.", cancellationToken: cancellationToken);
            return;
        }

        long userId;
        string role;

        if (message.ReplyToMessage != null)
        {
            userId = message.ReplyToMessage.From!.Id;
            var parts = message.Text!.Split(' ');

            if (parts.Length != 2)
            {
                await botClient.SendTextMessageAsync(chatId, "Invalid command format. Use: /setrole [role]", cancellationToken: cancellationToken);
                return;
            }

            role = parts[1];
        }
        else
        {
            var parts = message.Text!.Split(' ');
            if (parts.Length != 3 || string.IsNullOrEmpty(parts[1]))
            {
                await botClient.SendTextMessageAsync(chatId, "Invalid command format. Use: /setrole [username] [role] " +
                                "or reply to the user's message with the command /setrole [role]", cancellationToken: cancellationToken);
                return;
            }

            var username = parts[1].TrimStart('@');
            var profile = await FindProfileByUsernameAsync(username, dbContext, cancellationToken);
            if (profile == null)
            {
                await botClient.SendTextMessageAsync(chatId, "Profile not found.", cancellationToken: cancellationToken);
                return;
            }

            userId = profile.Id;
            role = parts[2];
        }

        var userProfile = await dbContext.Profiles.SingleOrDefaultAsync(p => p.Id == userId, cancellationToken);
        if (userProfile == null)
        {
            await botClient.SendTextMessageAsync(chatId, "Profile not found.", cancellationToken: cancellationToken);
            return;
        }

        userProfile.Role = role;
        dbContext.Profiles.Update(userProfile);
        await dbContext.SaveChangesAsync(cancellationToken);

        var displayName = !string.IsNullOrEmpty(userProfile.Username) ? $"@{userProfile.Username}" : userProfile.FirstName ?? "NoName";
        await botClient.SendTextMessageAsync(chatId, $"Role of user {displayName} is now {userProfile.Role}.", cancellationToken: cancellationToken);
        _logger.LogInformation("Role of user {DisplayName} updated to {UserRole}.", displayName, userProfile.Role);
    }

    public async Task BanCommandAsync(ITelegramBotClient botClient, Message message, long chatId, 
        BotDbContext dbContext, CancellationToken cancellationToken)
    {
        if (message.From?.Username != "arrogganz")
        {
            await botClient.SendTextMessageAsync(chatId, "You do not have permission to ban users.", cancellationToken: cancellationToken);
            return;
        }

        long userId;

        if (message.ReplyToMessage != null)
        {
            userId = message.ReplyToMessage.From!.Id;
        }
        else
        {
            var parts = message.Text!.Split(' ');
            if (parts.Length != 2 || string.IsNullOrEmpty(parts[1]))
            {
                await botClient.SendTextMessageAsync(chatId, "Invalid command format. Use: /ban [username] " +
                                "or reply to the user's message with the command /ban", cancellationToken: cancellationToken);
                return;
            }

            var username = parts[1].TrimStart('@');
            var profile = await FindProfileByUsernameAsync(username, dbContext, cancellationToken);
            if (profile == null)
            {
                await botClient.SendTextMessageAsync(chatId, "Profile not found.", cancellationToken: cancellationToken);
                return;
            }

            userId = profile.Id;
        }

        await botClient.BanChatMemberAsync(chatId, userId, cancellationToken: cancellationToken);
        await botClient.SendTextMessageAsync(chatId, "User has been banned.", cancellationToken: cancellationToken);
        _logger.LogInformation("User was banned.");
    }
    
    public async Task UnbanCommandAsync(ITelegramBotClient botClient, Message message, long chatId, 
        BotDbContext dbContext, CancellationToken cancellationToken)
    {
        if (message.From?.Username != "arrogganz")
        {
            await botClient.SendTextMessageAsync(chatId, "You do not have permission to unban users.", cancellationToken: cancellationToken);
            return;
        }

        long userId;

        if (message.ReplyToMessage != null)
        {
            userId = message.ReplyToMessage.From!.Id;
        }
        else
        {
            var parts = message.Text!.Split(' ');
            if (parts.Length != 2 || string.IsNullOrEmpty(parts[1]))
            {
                await botClient.SendTextMessageAsync(chatId, "Invalid command format. Use: /unban [username] " +
                                                             "or reply to the user's message with the command /unban", cancellationToken: cancellationToken);
                return;
            }

            var username = parts[1].TrimStart('@');
            var profile = await FindProfileByUsernameAsync(username, dbContext, cancellationToken);
            if (profile == null)
            {
                await botClient.SendTextMessageAsync(chatId, "Profile not found.", cancellationToken: cancellationToken);
                return;
            }

            userId = profile.Id;
        }

        await botClient.UnbanChatMemberAsync(chatId, userId, cancellationToken: cancellationToken);
        await botClient.SendTextMessageAsync(chatId, $"User has been unbanned.", cancellationToken: cancellationToken);
        _logger.LogInformation("User {UserId} was unbanned.", userId);
    }
    
    public async Task WelcomeNewMembersAsync(ITelegramBotClient botClient, Message message, long chatId, 
        CancellationToken cancellationToken, BotDbContext dbContext)
    {
        var tasks = message.NewChatMembers!
            .Where(newMember => newMember.Id != botClient.BotId)
            .Select(async newMember =>
            {
                await RegisterUserAsync(newMember, "Newbie-Developer", dbContext, cancellationToken);
                await botClient.SendTextMessageAsync(
                    chatId,
                    $"Welcome, {newMember.Username}!\nCan you please introduce yourself?\nIf you have any questions, feel free to ask.",
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
            _logger.LogError("Error saving changes: {ErrorMessage}", ex.Message);
        }
    }

    public async Task FarewellMemberAsync(ITelegramBotClient botClient, Message message, long chatId, CancellationToken cancellationToken)
    {
        var leftMember = message.LeftChatMember!;
        if (leftMember.Id != botClient.BotId)
        {
            var displayName = !string.IsNullOrEmpty(leftMember.Username) 
                ? $"@{leftMember.Username}" 
                : leftMember.FirstName;
            
            await botClient.SendTextMessageAsync(
                chatId,
                $"{leftMember.Username} left the chat. We hope they return soon!",
                replyToMessageId: message.MessageId,
                cancellationToken: cancellationToken
            );
            _logger.LogInformation("User {DisplayName} left the chat.", displayName);
        }
    }

    public async Task CleanUpDuplicateProfilesAsync(BotDbContext dbContext, CancellationToken cancellationToken)
    {
        var duplicates = dbContext.Profiles
            .AsEnumerable()
            .GroupBy(p => p.Id)
            .Where(g => g.Count() > 1)
            .SelectMany(g => g.Skip(1))
            .ToList();

        _logger.LogInformation("Found {DuplicatesCount} duplicate profiles", duplicates.Count);

        foreach (var duplicate in duplicates)
        {
            var mainProfile = await dbContext.Profiles
                .Where(p => p.Id == duplicate.Id)
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
    
    public async Task<List<Profile>> GetProfilesByRoleAsync(string role, BotDbContext dbContext, CancellationToken cancellationToken)
    {
        var normalizedRole = role.ToLower();
        return await dbContext.Profiles
            .Where(p => p.Role != null && p.Role.ToLower().Contains(normalizedRole))
            .ToListAsync(cancellationToken);
    }
    
    public async Task<List<string?>> GetAllRolesAsync(BotDbContext dbContext, CancellationToken cancellationToken)
    {
        var roles = await dbContext.Profiles
            .Where(p => p.Role != null)
            .Select(p => p.Role)
            .Distinct()
            .ToListAsync(cancellationToken);

        return roles;
    }
    
    private async Task<Profile?> FindProfileByUsernameAsync(string username, BotDbContext dbContext, CancellationToken cancellationToken)
    {
        return await dbContext.Profiles.FirstOrDefaultAsync(p => p.Username == username, cancellationToken);
    }
}