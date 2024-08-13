using CPK_Bot.Data.Context;
using CPK_Bot.Services.Commands.CommonCommands;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace CPK_Bot.Services.Commands.AdminCommands;

public class CleanupCommand : ICommand
{
    private readonly IProfileService _profileService;
    private readonly ILogger<CleanupCommand> _logger;

    public CleanupCommand(IProfileService profileService, ILogger<CleanupCommand> logger)
    {
        _profileService = profileService;
        _logger = logger;
    }
    
    public async Task ExecuteAsync(ITelegramBotClient botClient, Message message, long chatId, BotDbContext dbContext,
        CancellationToken cancellationToken)
    {
        await botClient.SendTextMessageAsync(chatId, "Starting cleanup...", cancellationToken: cancellationToken);

        await _profileService.CleanUpDuplicateProfilesAsync(dbContext, cancellationToken);
        await botClient.SendTextMessageAsync(chatId, "Cleanup completed.", cancellationToken: cancellationToken);
        _logger.LogInformation("Cleanup command executed successfully.");
    }
}