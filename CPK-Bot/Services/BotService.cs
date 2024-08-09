using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;

namespace CPK_Bot.Services;

public class BotService
{
    private readonly TelegramBotClient _bot;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BotService> _logger;
    private readonly CancellationTokenSource _cts = new();

    public BotService(TelegramBotClient bot, IServiceProvider serviceProvider, ILogger<BotService> logger)
    {
        _bot = bot;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public void Start()
    {
        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = []
        };

        _bot.StartReceiving(
            async (botClient, update, cancellationToken) =>
            {
                using var scope = _serviceProvider.CreateScope();
                var updateHandler = scope.ServiceProvider.GetRequiredService<UpdateHandler>();
                await updateHandler.HandleUpdateAsync(botClient, update, cancellationToken);
            },
            HandleErrorAsync,
            receiverOptions,
            _cts.Token
        );

        _logger.LogInformation("Bot is up and running...");
        Console.ReadLine();
        _cts.Cancel();
    }

    private Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        var errorMessage = exception switch
        {
            ApiRequestException apiRequestException => 
                $"Telegram API Error: [Error Code: {apiRequestException.ErrorCode}] Message: {apiRequestException.Message}",
            _ => $"An unexpected error occurred: {exception.Message}"
        };

        _logger.LogError("Exception occurred: {ExceptionType}, Error Message: {ErrorMessage}", exception.GetType().Name, errorMessage);

        return Task.CompletedTask;
    }
}