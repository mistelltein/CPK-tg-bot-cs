using CPK_Bot.Data.Context;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace CPK_Bot.Services.Commands.CommonCommands;

public class WeatherCommand : ICommand
{
    private readonly IWeatherService _weatherService;
    private readonly ILogger<WeatherCommand> _logger;

    public WeatherCommand(IWeatherService weatherService, ILogger<WeatherCommand> logger)
    {
        _weatherService = weatherService;
        _logger = logger;
    }
    
    public async Task ExecuteAsync(ITelegramBotClient botClient, Message message, long chatId, BotDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var parts = message.Text?.Split(' ');
        var location = parts?.Skip(1).FirstOrDefault();

        if (string.IsNullOrEmpty(location))
        {
            await botClient.SendTextMessageAsync(chatId, "Please provide a location. Example: /weather London", cancellationToken: cancellationToken);
            return;
        }

        try
        {
            var weatherInfo = await _weatherService.GetWeatherAsync(location);
            await botClient.SendTextMessageAsync(chatId, weatherInfo, parseMode: ParseMode.Markdown, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching weather data for location {Location}", location);
            await botClient.SendTextMessageAsync(chatId, "Failed to fetch weather data. Please try again later.", cancellationToken: cancellationToken);
        }
    }
}