using CPK_Bot.Data.Context;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace CPK_Bot.Services;

public class UpdateHandler
{
    private readonly ILogger<UpdateHandler> _logger;
    private readonly IServiceProvider _serviceProvider;

    public UpdateHandler(IServiceProvider serviceProvider, ILogger<UpdateHandler> logger)
    {
        this._logger = logger;
        this._serviceProvider = serviceProvider;
    }

    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received update of type {UpdateType}", update.Type);

        try
        {
            if (update is { Type: UpdateType.Message, Message: not null })
            {
                var message = update.Message;
                var chatId = message.Chat.Id;

                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<BotDbContext>();
                var commandHandler = scope.ServiceProvider.GetRequiredService<CommandHandler>();

                if (message.Text is not null)
                {
                    await commandHandler.HandleTextMessageAsync(botClient, message, chatId, dbContext, cancellationToken);
                }

                await commandHandler.HandleMessageTypeAsync(botClient, message, chatId, dbContext, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling update");
            throw; // Re-throw the exception after logging it
        }

        _logger.LogInformation("Update processed successfully");
    }
}