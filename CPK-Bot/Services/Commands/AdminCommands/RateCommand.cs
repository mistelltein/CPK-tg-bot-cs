using CPK_Bot.Data.Context;
using CPK_Bot.Services.Commands.CommonCommands;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace CPK_Bot.Services.Commands.AdminCommands;

public class RateCommand : ICommand
{
    private readonly IProfileService _profileService;

    public RateCommand(IProfileService profileService)
    {
        _profileService = profileService;
    }
    
    public async Task ExecuteAsync(ITelegramBotClient botClient, Message message, long chatId, BotDbContext dbContext,
        CancellationToken cancellationToken)
    {
        await _profileService.RateCommandAsync(botClient, message, chatId, dbContext, cancellationToken);
    }
}