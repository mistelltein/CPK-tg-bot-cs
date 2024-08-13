using CPK_Bot.Data.Context;
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
            if (update.Message is not null)
            {
                var message = update.Message;
                var chatId = message.Chat.Id;

                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<BotDbContext>();
                var commandHandler = scope.ServiceProvider.GetRequiredService<ICommandHandler>();

                await commandHandler.HandleUpdateAsync(botClient, message, chatId, dbContext, cancellationToken);
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