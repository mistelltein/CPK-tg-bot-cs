using CPK_Bot.Data.Context;
using CPK_Bot.Services.Commands.CommonCommands;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace CPK_Bot.Services.Commands.AdminCommands;

public class SetRoleCommand : ICommand
{
    private readonly IProfileService _profileService;

    public SetRoleCommand(IProfileService profileService)
    {
        _profileService = profileService;
    }
    
    public async Task ExecuteAsync(ITelegramBotClient botClient, Update update, long chatId, BotDbContext dbContext,
        CancellationToken cancellationToken)
    {
        await _profileService.SetRoleCommandAsync(botClient, update.Message!, chatId, dbContext, cancellationToken);
    }
}