using CPK_Bot.Data.Context;
using CPK_Bot.Services.Commands.CommonCommands;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace CPK_Bot.Services.Commands.UserCommands;

public class FindUserCommand : ICommand
{
    private readonly IProfileService _profileService;
    private readonly ILogger<FindUserCommand> _logger;

    public FindUserCommand(IProfileService profileService, ILogger<FindUserCommand> logger)
    {
        _profileService = profileService;
        _logger = logger;
    }
    
    public async Task ExecuteAsync(ITelegramBotClient botClient, Message message, long chatId, BotDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var parts = message.Text?.Split(' ');

        if (parts is { Length: 2 })
        {
            var username = parts[1].TrimStart('@');
            try
            {
                await _profileService.ShowProfileByUsernameAsync(botClient, chatId, username, dbContext, cancellationToken);
                _logger.LogInformation("Profile of user {Username} displayed successfully.", username);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while displaying profile for user {Username}.", username);
                await botClient.SendTextMessageAsync(chatId, "An error occurred while processing your request.", cancellationToken: cancellationToken);
            }
        }
        else
        {
            _logger.LogWarning("Invalid format for /finduser command.");
            await botClient.SendTextMessageAsync(chatId, "Invalid command format. Use: /finduser @username", cancellationToken: cancellationToken);
        }
    }
}