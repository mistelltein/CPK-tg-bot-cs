using CPK_Bot.Data.Context;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace CPK_Bot.Services.Commands.CommonCommands;

public class HandleBotCommand : ICommand
{
    public async Task ExecuteAsync(ITelegramBotClient botClient, Message message, long chatId, BotDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var response = message.Chat.Type switch
        {
            ChatType.Private => "Hi! How can I help you?",
            ChatType.Supergroup => "How can I assist you?",
            _ => "Hello!"
        };

        await botClient.SendTextMessageAsync(chatId, response, cancellationToken: cancellationToken);
    }
}