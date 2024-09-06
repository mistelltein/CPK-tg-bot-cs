using CPK_Bot.Data.Context;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace CPK_Bot.Services.Commands.CommonCommands;

public class HandleMessageTypeCommand : ICommand
{
    private readonly IProfileService _profileService;

    public HandleMessageTypeCommand(IProfileService profileService)
    {
        _profileService = profileService;
    }
    
    public async Task ExecuteAsync(ITelegramBotClient botClient, Update update, long chatId, BotDbContext dbContext,
        CancellationToken cancellationToken)
    {
        switch (update.Message!.Type)
        {
            case MessageType.ChatMembersAdded:
                await _profileService.WelcomeNewMembersAsync(botClient, update.Message!, chatId, cancellationToken, dbContext);
                break;
            case MessageType.ChatMemberLeft when update.Message!.LeftChatMember is not null:
                await _profileService.FarewellMemberAsync(botClient, update.Message!, chatId, cancellationToken);
                break;
        }
    }
}