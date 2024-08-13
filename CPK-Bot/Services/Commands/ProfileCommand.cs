using CPK_Bot.Data.Context;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace CPK_Bot.Services.Commands;

public class ProfileCommand : ICommand
{
    private readonly ProfileService _profileService;

    public ProfileCommand(ProfileService profileService)
    {
        _profileService = profileService;
    }
    
    public async Task ExecuteAsync(ITelegramBotClient botClient, Message message, long chatId, BotDbContext dbContext,
        CancellationToken cancellationToken)
    {
        await _profileService.ShowProfileAsync(botClient, chatId, message.From!.Id, dbContext, cancellationToken);
    }
}