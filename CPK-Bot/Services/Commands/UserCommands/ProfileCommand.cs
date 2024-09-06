using CPK_Bot.Data.Context;
using CPK_Bot.Services.Commands.CommonCommands;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace CPK_Bot.Services.Commands.UserCommands;

public class ProfileCommand : ICommand
{
    private readonly IProfileService _profileService;

    public ProfileCommand(IProfileService profileService)
    {
        _profileService = profileService;
    }
    
    public async Task ExecuteAsync(ITelegramBotClient botClient, Update update, long chatId, BotDbContext dbContext,
        CancellationToken cancellationToken)
    {
        await _profileService.ShowProfileAsync(botClient, chatId, update.Message!.From!.Id, dbContext, cancellationToken);
    }
}