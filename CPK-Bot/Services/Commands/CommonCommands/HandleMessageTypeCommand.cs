using CPK_Bot.Data.Context;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace CPK_Bot.Services.Commands.CommonCommands;

public class HandleMessageTypeCommand : ICommand
{
    private readonly ProfileService _profileService;

    public HandleMessageTypeCommand(ProfileService profileService)
    {
        _profileService = profileService;
    }
    
    public async Task ExecuteAsync(ITelegramBotClient botClient, Message message, long chatId, BotDbContext dbContext,
        CancellationToken cancellationToken)
    {
        switch (message.Type)
        {
            case MessageType.ChatMembersAdded:
                await _profileService.WelcomeNewMembersAsync(botClient, message, chatId, cancellationToken, dbContext);
                break;
            case MessageType.ChatMemberLeft when message.LeftChatMember is not null:
                await _profileService.FarewellMemberAsync(botClient, message, chatId, cancellationToken);
                break;
        }
    }
}