using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;

namespace CPK_Bot.Services;

public class BotService(ITelegramBotClient bot, IServiceProvider serviceProvider, ILogger<BotService> logger)
{
    private readonly CancellationTokenSource _cts = new();

    public void Start()
    {
        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = []
        };

        bot.StartReceiving(
            async (botClient, update, cancellationToken) =>
            {
                using var scope = serviceProvider.CreateScope();
                var updateHandler = scope.ServiceProvider.GetRequiredService<UpdateHandler>();
                await updateHandler.HandleUpdateAsync(botClient, update, cancellationToken);
            },
            HandleErrorAsync,
            receiverOptions,
            _cts.Token
        );

        Console.WriteLine("Bot is up and running...");
        Console.ReadLine();
        _cts.Cancel();
    }

    private Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        var errorMessage = exception switch
        {
            ApiRequestException apiRequestException => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => exception.ToString()
        };

        logger.LogError(errorMessage);
        return Task.CompletedTask;
    }
}