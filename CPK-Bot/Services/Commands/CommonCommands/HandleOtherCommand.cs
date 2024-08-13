using CPK_Bot.Data.Context;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace CPK_Bot.Services.Commands.CommonCommands;

public class HandleOtherCommand : ICommand
{
    private readonly ILogger<HandleOtherCommand> _logger;

    public HandleOtherCommand(ILogger<HandleOtherCommand> logger)
    {
        _logger = logger;
    }
    
    public Task ExecuteAsync(ITelegramBotClient botClient, Message message, long chatId, BotDbContext dbContext,
        CancellationToken cancellationToken)
    {
        _logger.LogWarning("Unknown command received: {Command}", message.Text);
        return Task.CompletedTask;
    }
}