using CPK_Bot.Data.Context;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace CPK_Bot.Services;

public class UpdateHandler(IServiceProvider serviceProvider, ILogger<UpdateHandler> logger)
{
    private readonly ILogger<UpdateHandler> _logger = logger;

    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update is { Type: UpdateType.Message, Message: not null })
        {
            var message = update.Message;
            var chatId = message.Chat.Id;

            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<BotDbContext>();
            var commandHandler = scope.ServiceProvider.GetRequiredService<CommandHandler>();

            if (message.Text is not null)
            {
                await commandHandler.HandleTextMessageAsync(botClient, message, chatId, dbContext, cancellationToken);
            }

            await commandHandler.HandleMessageTypeAsync(botClient, message, chatId, dbContext, cancellationToken);
        }
    }
}