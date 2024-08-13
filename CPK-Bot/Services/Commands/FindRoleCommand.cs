using CPK_Bot.Data.Context;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace CPK_Bot.Services.Commands;

public class FindRoleCommand : ICommand
{
    private readonly ProfileService _profileService;
    private readonly ILogger<FindRoleCommand> _logger;
    
    public FindRoleCommand(ProfileService profileService, ILogger<FindRoleCommand> logger)
    {
        _profileService = profileService;
        _logger = logger;
    }
    
    public async Task ExecuteAsync(ITelegramBotClient botClient, Message message, long chatId, BotDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var parts = message.Text?.Split(' ');
        var role = parts?.Skip(1).FirstOrDefault();

        if (string.IsNullOrEmpty(role))
        {
            await botClient.SendTextMessageAsync(chatId, "Please provide a role. Example: /findrole Python-Developer", cancellationToken: cancellationToken);
            return;
        }

        try
        {
            var profiles = await _profileService.GetProfilesByRoleAsync(role, dbContext, cancellationToken);
            
            if (profiles.Count > 0)
            {
                var response = string.Join("\n", profiles.Select(p => $"@{p.Username} - {p.FirstName}"));
                await botClient.SendTextMessageAsync(chatId, $"Found the following users with role {role}:\n{response}", cancellationToken: cancellationToken);
            }
            else
            {
                await botClient.SendTextMessageAsync(chatId, $"No users found with role {role}.", cancellationToken: cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching profiles by role {Role}.", role);
            await botClient.SendTextMessageAsync(chatId, "Failed to fetch profiles by role. Please try again later.", cancellationToken: cancellationToken);
        }
    }
}