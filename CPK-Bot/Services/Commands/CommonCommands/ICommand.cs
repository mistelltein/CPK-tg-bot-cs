using CPK_Bot.Data.Context;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace CPK_Bot.Services.Commands.CommonCommands;

public interface  ICommand
{
    Task ExecuteAsync(ITelegramBotClient botClient, Update update, long chatId, BotDbContext dbContext, 
        CancellationToken cancellationToken);
}