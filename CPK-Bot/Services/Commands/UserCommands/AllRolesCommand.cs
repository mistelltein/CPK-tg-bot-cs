using CPK_Bot.Data.Context;
using CPK_Bot.Services.Commands.CommonCommands;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace CPK_Bot.Services.Commands.UserCommands;

public class AllRolesCommand : ICommand
{
    private readonly IProfileService _profileService;
    private readonly ILogger<AllRolesCommand> _logger;

    public AllRolesCommand(IProfileService profileService, ILogger<AllRolesCommand> logger)
    {
        _profileService = profileService;
        _logger = logger;
    }
    
    public async Task ExecuteAsync(ITelegramBotClient botClient, Message message, long chatId, BotDbContext dbContext,
        CancellationToken cancellationToken)
    {
        try
        {
            var roles = await _profileService.GetAllRolesAsync(dbContext, cancellationToken);
            
            if (roles.Count != 0)
            {
                var rolesList = string.Join("\n- ", roles.Prepend("Available roles:"));
                var formattedMessage = $"*{rolesList}*";

                await botClient.SendTextMessageAsync(
                    chatId,
                    formattedMessage,
                    parseMode: ParseMode.Markdown,
                    cancellationToken: cancellationToken
                );
            }
            else
            {
                await botClient.SendTextMessageAsync(chatId, "No roles found.", cancellationToken: cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Error fetching roles: {ErrorMessage}", ex.Message);
            await botClient.SendTextMessageAsync(chatId, "Failed to fetch roles. Please try again later.", 
                cancellationToken: cancellationToken);
        }
    }
}