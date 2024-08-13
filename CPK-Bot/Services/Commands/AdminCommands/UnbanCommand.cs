using CPK_Bot.Data.Context;
using CPK_Bot.Services.Commands.CommonCommands;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace CPK_Bot.Services.Commands.AdminCommands;

public class UnbanCommand : ICommand
{
    private readonly IProfileService _profileService;

    public UnbanCommand(IProfileService profileService)
    {
        _profileService = profileService;
    }
    
    public async Task ExecuteAsync(ITelegramBotClient botClient, Message message, long chatId, BotDbContext dbContext,
        CancellationToken cancellationToken)
    {
        await _profileService.UnbanCommandAsync(botClient, message, chatId, dbContext, cancellationToken);
    }
}