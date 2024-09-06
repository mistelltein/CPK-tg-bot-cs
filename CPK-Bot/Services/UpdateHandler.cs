using CPK_Bot.Data.Context;
using CPK_Bot.Services.Commands.CommonCommands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace CPK_Bot.Services;

public interface IUpdateHandler
{
    Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken);
}

public class UpdateHandler : IUpdateHandler
{
    private readonly ILogger<UpdateHandler> _logger;
    private readonly IServiceProvider _serviceProvider;

    public UpdateHandler(IServiceProvider serviceProvider, ILogger<UpdateHandler> logger)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received update of type {UpdateType}", update.Type);

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<BotDbContext>();
            var commandFactory = scope.ServiceProvider.GetRequiredService<ICommandFactory>();

            if (update.Message is not null)
            {
                var message = update.Message;
                var chatId = message.Chat.Id;

                _logger.LogInformation("Processing message update for chat {ChatId} from user {UserId}", chatId, message.From?.Id);

                var command = commandFactory.GetCommand(update);
                await command.ExecuteAsync(botClient, update, chatId, dbContext, cancellationToken);
            }
            else if (update.ChatMember is not null)
            {
                var chatMemberUpdated = update.ChatMember;
                var chatId = chatMemberUpdated.Chat.Id;

                _logger.LogInformation("Processing chat member update for chat {ChatId} with new status {NewStatus}", chatId, chatMemberUpdated.NewChatMember.Status);

                var command = commandFactory.GetCommand(update);
                await command.ExecuteAsync(botClient, update, chatId, dbContext, cancellationToken);
            }
            else
            {
                _logger.LogWarning("Unhandled update type: {UpdateType}", update.Type);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling update");
            throw;
        }

        _logger.LogInformation("Update processed successfully");
    }
}