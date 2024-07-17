using CPK_Bot.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace CPK_Bot.Services;

public class CommandHandler
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CommandHandler> _logger;
    private readonly WeatherService _weatherService;

    public CommandHandler(IServiceProvider serviceProvider, ILogger<CommandHandler> logger, WeatherService weatherService)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _weatherService = weatherService;
    }

    public async Task HandleTextMessageAsync(ITelegramBotClient botClient, Message message, long chatId, BotDbContext dbContext, CancellationToken cancellationToken)
    {
        var profileService = _serviceProvider.GetRequiredService<ProfileService>();
        var questionService = _serviceProvider.GetRequiredService<QuestionService>();

        if (message.From != null)
        {
            await profileService.RegisterUserAsync(message.From, "Newbie-Developer", dbContext, cancellationToken);
        }

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation($"User {message.From?.Username} registered/updated successfully.");
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError($"Error saving changes: {ex.Message}");
            await botClient.SendTextMessageAsync(chatId, "Произошла ошибка при сохранении изменений.", cancellationToken: cancellationToken);
            return;
        }

        if (message.Text != null)
        {
            _logger.LogInformation($"Received text message: {message.Text}");
        }

        switch (message.Chat.Type)
        {
            case ChatType.Private when message.Text!.Equals("бот", StringComparison.CurrentCultureIgnoreCase):
                await botClient.SendTextMessageAsync(chatId, "Привет! Как я могу помочь тебе?", cancellationToken: cancellationToken);
                break;
            case ChatType.Supergroup when message.Text!.Equals("бот", StringComparison.CurrentCultureIgnoreCase):
                await botClient.SendTextMessageAsync(chatId, "Чем я могу вам помочь?", cancellationToken: cancellationToken);
                break;
        }

        if (message.Text != null)
        {
            await HandleCommandAsync(botClient, message, chatId, dbContext, profileService, questionService, cancellationToken);
        }
    }

    private async Task HandleCommandAsync(ITelegramBotClient botClient, Message message, long chatId, BotDbContext dbContext, ProfileService profileService, QuestionService questionService, CancellationToken cancellationToken)
    {
        var command = message.Text!.ToLower();

        try
        {
            switch (command)
            {
                case "/start":
                    await botClient.SendTextMessageAsync(
                        chatId,
                        "Привет, вот мои команды:\n" +
                        "/profile\n" +
                        "/givebackendquestion\n" +
                        "/givefrontendquestion\n" +
                        "/finduser @username\n",
                        cancellationToken: cancellationToken
                    );
                    break;

                case "/profile@it_kyrgyzstan_cs_bot":
                case "/profile":
                    await profileService.ShowProfileAsync(botClient, chatId, message.From!.Id, dbContext, cancellationToken);
                    break;

                case var cmd when cmd.StartsWith("/addbackendquestion"):
                    await questionService.AddBackendQuestionAsync(botClient, chatId, message.Text, dbContext, cancellationToken, message);
                    break;

                case var cmd when cmd.StartsWith("/backendquestions"):
                    await questionService.ListBackendQuestionsAsync(botClient, chatId, dbContext, cancellationToken);
                    break;

                case var cmd when cmd.StartsWith("/givebackendquestion"):
                    await questionService.GiveBackendQuestionAsync(botClient, chatId, dbContext, cancellationToken);
                    break;

                case var cmd when cmd.StartsWith("/addfrontendquestion"):
                    await questionService.AddFrontendQuestionAsync(botClient, chatId, message.Text, dbContext, cancellationToken, message);
                    break;

                case var cmd when cmd.StartsWith("/frontendquestions"):
                    await questionService.ListFrontendQuestionsAsync(botClient, chatId, dbContext, cancellationToken);
                    break;

                case var cmd when cmd.StartsWith("/givefrontendquestion"):
                    await questionService.GiveFrontendQuestionAsync(botClient, chatId, dbContext, cancellationToken);
                    break;

                case "/cleanup":
                    await HandleCleanupCommandAsync(botClient, chatId, cancellationToken);
                    break;

                case var cmd when cmd.StartsWith("/finduser"):
                    await HandleFindUserCommandAsync(botClient, chatId, message.Text, profileService, dbContext, cancellationToken);
                    break;
                
                case var cmd when cmd.StartsWith("/weather"):
                    await HandleWeatherCommandAsync(botClient, chatId, cmd, cancellationToken);
                    break;
                
                case var cmd when cmd.StartsWith("/findrole"):
                    await HandleFindRoleCommandAsync(botClient, chatId, cmd, profileService, dbContext, cancellationToken);
                    break;
                
                default:
                    await HandleCustomCommandAsync(botClient, message, chatId, dbContext, profileService, cancellationToken);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error handling command {command}: {ex.Message}");
            await botClient.SendTextMessageAsync(chatId, "Произошла ошибка при обработке команды.", cancellationToken: cancellationToken);
        }
    }

    private async Task HandleCleanupCommandAsync(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
    {
        await botClient.SendTextMessageAsync(chatId, "Starting cleanup...", cancellationToken: cancellationToken);

        using (var scope = _serviceProvider.CreateScope())
        {
            var scopedProfileService = scope.ServiceProvider.GetRequiredService<ProfileService>();
            var scopedDbContext = scope.ServiceProvider.GetRequiredService<BotDbContext>();

            await scopedProfileService.CleanUpDuplicateProfilesAsync(scopedDbContext, cancellationToken);
        }

        await botClient.SendTextMessageAsync(chatId, "Cleanup completed.", cancellationToken: cancellationToken);
        _logger.LogInformation("Cleanup command executed successfully.");
    }

    private async Task HandleFindUserCommandAsync(ITelegramBotClient botClient, long chatId, string messageText, ProfileService profileService, BotDbContext dbContext, CancellationToken cancellationToken)
    {
        var parts = messageText.Split(' ');
        if (parts.Length == 2)
        {
            var username = parts[1].TrimStart('@');
            await profileService.ShowProfileByUsernameAsync(botClient, chatId, username, dbContext, cancellationToken);
            _logger.LogInformation($"Profile of user {username} displayed successfully.");
        }
        else
        {
            await botClient.SendTextMessageAsync(chatId, "Неверный формат команды. Используйте: /finduser @username", cancellationToken: cancellationToken);
            _logger.LogWarning("Invalid format for /finduser command.");
        }
    }

    private async Task HandleCustomCommandAsync(ITelegramBotClient botClient, Message message, long chatId, BotDbContext dbContext, ProfileService profileService, CancellationToken cancellationToken)
    {
        if (message.Text!.StartsWith("/rate"))
        {
            await profileService.HandleRateCommandAsync(botClient, message, chatId, dbContext, cancellationToken);
        }
        else if (message.Text!.StartsWith("/setrole"))
        {
            await profileService.HandleSetRoleCommandAsync(botClient, message, chatId, dbContext, cancellationToken);
        }
        else if (message.Text!.StartsWith("/ban"))
        {
            await profileService.HandleBanCommandAsync(botClient, message, chatId, dbContext, cancellationToken);
        }
        else
        {
            _logger.LogWarning($"Unknown command received: {message.Text}");
        }
    }

    public async Task HandleMessageTypeAsync(ITelegramBotClient botClient, Message message, long chatId, BotDbContext dbContext, CancellationToken cancellationToken)
    {
        var profileService = _serviceProvider.GetRequiredService<ProfileService>();

        switch (message.Type)
        {
            case MessageType.ChatMembersAdded:
                await profileService.WelcomeNewMembersAsync(botClient, message, chatId, cancellationToken, dbContext);
                break;
            case MessageType.ChatMemberLeft when message.LeftChatMember is not null:
                await profileService.FarewellMemberAsync(botClient, message, chatId, cancellationToken);
                break;
        }
    }
    
    private async Task HandleWeatherCommandAsync(ITelegramBotClient botClient, long chatId, string cmd, CancellationToken cancellationToken)
    {
        var location = cmd.Split(' ').Skip(1).FirstOrDefault();
        if (string.IsNullOrEmpty(location))
        {
            await botClient.SendTextMessageAsync(chatId, "Please provide a location. Example: /weather London", cancellationToken: cancellationToken);
        }
        else
        {
            try
            {
                var weatherInfo = await _weatherService.GetWeatherAsync(location);
                await botClient.SendTextMessageAsync(chatId, weatherInfo, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching weather data: {ex.Message}");
                await botClient.SendTextMessageAsync(chatId, "Failed to fetch weather data. Please try again later.", cancellationToken: cancellationToken);
            }
        }
    }

    private async Task HandleFindRoleCommandAsync(ITelegramBotClient botClient, long chatId, string cmd, ProfileService profileService, BotDbContext dbContext, CancellationToken cancellationToken)
    {
        var role = cmd.Split(' ').Skip(1).FirstOrDefault();
        if (string.IsNullOrEmpty(role))
        {
            await botClient.SendTextMessageAsync(chatId, "Please provide a role. Example: /findrole Python-Developer", cancellationToken: cancellationToken);
        }
        else
        {
            try
            {
                var profiles = await profileService.GetProfilesByRoleAsync(role, dbContext, cancellationToken);
                if (profiles.Count != 0)
                {
                    var response = string.Join("\n", profiles.Select(p => $"@{p.Username} - {p.FirstName}"));
                    await botClient.SendTextMessageAsync(chatId, $"Found the following users with role {role}:\n{response}", cancellationToken: cancellationToken);
                }
                else
                {
                    await botClient.SendTextMessageAsync(chatId, $"No users found with role {role}.", cancellationToken: cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching profiles by role: {ex.Message}");
                await botClient.SendTextMessageAsync(chatId, "Failed to fetch profiles by role. Please try again later.", cancellationToken: cancellationToken);
            }
        }
    }
}