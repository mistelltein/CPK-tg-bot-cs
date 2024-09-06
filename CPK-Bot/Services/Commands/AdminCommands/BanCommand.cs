using CPK_Bot.Data.Context;
using CPK_Bot.Services.Commands.CommonCommands;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace CPK_Bot.Services.Commands.AdminCommands;

public class BanCommand : ICommand
{
    private readonly IProfileService _profileService;

    public BanCommand(IProfileService profileService)
    {
        _profileService = profileService;
    }
    
    public async Task ExecuteAsync(ITelegramBotClient botClient, Update update, long chatId, BotDbContext dbContext,
        CancellationToken cancellationToken)
    {
        await _profileService.BanCommandAsync(botClient, update.Message!, chatId, dbContext, cancellationToken);
    }
}