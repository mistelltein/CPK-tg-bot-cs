using CPK_Bot.Data.Context;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace CPK_Bot.Services.Commands;

public class HandleOtherCommand : ICommand
{
    private readonly ILogger _logger;

    public HandleOtherCommand(ILogger<HandleOtherCommand> logger)
    {
        _logger = logger;
    }
    
    public async Task ExecuteAsync(ITelegramBotClient botClient, Message message, long chatId, BotDbContext dbContext,
        CancellationToken cancellationToken)
    {
        _logger.LogWarning("Unknown command received: {Command}", message.Text);
    }
}