using CPK_Bot.Data.Context;
using CPK_Bot.Services.Commands.CommonCommands;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace CPK_Bot.Services;

public interface ICommandHandler
{
    Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, long chatId, BotDbContext dbContext,
        CancellationToken cancellationToken);
}

public class CommandHandler : ICommandHandler
{
    private readonly ICommandFactory _commandFactory;
    private readonly ILogger<CommandHandler> _logger;
    private readonly IProfileService _profileService;

    public CommandHandler(ICommandFactory commandFactory, ILogger<CommandHandler> logger, IProfileService profileService)
    {
        _commandFactory = commandFactory;
        _logger = logger;
        _profileService = profileService;
    }

    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, long chatId, BotDbContext dbContext, 
        CancellationToken cancellationToken)
    {
        if (update.Message!.From != null)
        {
            await _profileService.RegisterUserAsync(update.Message!.From, "Newbie-Developer", dbContext, cancellationToken);
        }

        _logger.LogInformation("Received message of type {MessageType}: {MessageText}", update.Message!.Type, update.Message!.Text);

        var command = _commandFactory.GetCommand(update);

        await command.ExecuteAsync(botClient, update, chatId, dbContext, cancellationToken);
    }
}