using CPK_Bot.Data.Context;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace CPK_Bot.Services.Commands;

public class SetRoleCommand : ICommand
{
    private readonly ProfileService _profileService;

    public SetRoleCommand(ProfileService profileService)
    {
        _profileService = profileService;
    }
    
    public async Task ExecuteAsync(ITelegramBotClient botClient, Message message, long chatId, BotDbContext dbContext,
        CancellationToken cancellationToken)
    {
        await _profileService.SetRoleCommandAsync(botClient, message, chatId, dbContext, cancellationToken);
    }
}