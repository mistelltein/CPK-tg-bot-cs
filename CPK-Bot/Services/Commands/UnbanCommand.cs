using CPK_Bot.Data.Context;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace CPK_Bot.Services.Commands;

public class UnbanCommand : ICommand
{
    private readonly ProfileService _profileService;

    public UnbanCommand(ProfileService profileService)
    {
        _profileService = profileService;
    }
    
    public async Task ExecuteAsync(ITelegramBotClient botClient, Message message, long chatId, BotDbContext dbContext,
        CancellationToken cancellationToken)
    {
        await _profileService.UnbanCommandAsync(botClient, message, chatId, dbContext, cancellationToken);
    }
}