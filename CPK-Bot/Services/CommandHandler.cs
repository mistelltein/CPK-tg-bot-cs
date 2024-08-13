using CPK_Bot.Data.Context;
using CPK_Bot.Services.Commands.CommonCommands;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace CPK_Bot.Services;

public class CommandHandler
{
    private readonly CommandFactory _commandFactory;
    private readonly ILogger<CommandHandler> _logger;
    private readonly ProfileService _profileService;

    public CommandHandler(CommandFactory commandFactory, ILogger<CommandHandler> logger, ProfileService profileService)
    {
        _commandFactory = commandFactory;
        _logger = logger;
        _profileService = profileService;
    }

    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Message message, long chatId, BotDbContext dbContext, CancellationToken cancellationToken)
    {
        if (message.From != null)
        {
            await _profileService.RegisterUserAsync(message.From, "Newbie-Developer", dbContext, cancellationToken);
        }

        _logger.LogInformation("Received message of type {MessageType}: {MessageText}", message.Type, message.Text);

        var command = _commandFactory.GetCommand(message);

        await command.ExecuteAsync(botClient, message, chatId, dbContext, cancellationToken);
    }
}