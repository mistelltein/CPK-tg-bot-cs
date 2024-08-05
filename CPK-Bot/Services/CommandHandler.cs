using CPK_Bot.Data.Context;
using CPK_Bot.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace CPK_Bot.Services;

public class CommandHandler
{
    private readonly ProfileService _profileService;
    private readonly QuestionService _questionService;
    private readonly ILogger<CommandHandler> _logger;
    private readonly WeatherService _weatherService;
    private readonly QuizService _quizService;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public CommandHandler(ProfileService profileService, QuestionService questionService, ILogger<CommandHandler> logger, 
        WeatherService weatherService, QuizService quizService, IServiceScopeFactory serviceScopeFactory)
    {
        _logger = logger;
        _weatherService = weatherService;
        _quizService = quizService;
        _profileService = profileService;
        _questionService = questionService;
        _serviceScopeFactory = serviceScopeFactory;
    }

    public async Task HandleTextMessageAsync(ITelegramBotClient botClient, Message message, long chatId, 
        BotDbContext dbContext, CancellationToken cancellationToken)
    {
        if (message.From != null)
        {
            await _profileService.RegisterUserAsync(message.From, "Newbie-Developer", dbContext, cancellationToken);
        }

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation($"User {message.From?.Username} registered/updated successfully.");
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError($"Error saving changes: {ex.Message}");
            await botClient.SendTextMessageAsync(chatId, "An error occurred while saving changes.", cancellationToken: cancellationToken);
            return;
        }

        if (message.Text != null)
        {
            _logger.LogInformation($"Received text message: {message.Text}");
        }

        switch (message.Chat.Type)
        {
            case ChatType.Private when message.Text!.Equals("бот", StringComparison.CurrentCultureIgnoreCase):
                await botClient.SendTextMessageAsync(chatId, "Hi! How can I help you?", cancellationToken: cancellationToken);
                break;
            case ChatType.Supergroup when message.Text!.Equals("бот", StringComparison.CurrentCultureIgnoreCase):
                await botClient.SendTextMessageAsync(chatId, "How can I assist you?", cancellationToken: cancellationToken);
                break;
        }

        if (message.Text != null)
        {
            await HandleCommandAsync(botClient, message, chatId, dbContext, _profileService, _questionService, cancellationToken);
        }
    }

    private async Task HandleCommandAsync(ITelegramBotClient botClient, Message message, long chatId, 
        BotDbContext dbContext, ProfileService profileService, QuestionService questionService, CancellationToken cancellationToken)
    {
        var command = message.Text!.ToLower();

        try
        {
            switch (command)
            {
                case "/start":
                    await HandleStartCommand(botClient, chatId, cancellationToken);
                    break;
                
                case "/commands@it_kyrgyzstan_cs_bot":
                case "/commands":
                    await HandleCommandsCommand(botClient, chatId, cancellationToken);
                    break;

                case "/profile@it_kyrgyzstan_cs_bot":
                case "/profile":
                    await profileService.ShowProfileAsync(botClient, chatId, message.From!.Id, dbContext, cancellationToken);
                    break;

                case var cmd when cmd.StartsWith("/addbackendquestion"):
                    await questionService.AddQuestionAsync<BackendQuestion>(botClient, chatId, message.Text, dbContext, cancellationToken, message);
                    break;

                case var cmd when cmd.StartsWith("/backendquestions"):
                    await questionService.ListQuestionsAsync<BackendQuestion>(botClient, chatId, dbContext, cancellationToken);
                    break;

                case var cmd when cmd.StartsWith("/givebackendquestion"):
                    await questionService.GiveQuestionAsync<BackendQuestion>(botClient, chatId, dbContext, cancellationToken);
                    break;

                case var cmd when cmd.StartsWith("/addfrontendquestion"):
                    await questionService.AddQuestionAsync<FrontendQuestion>(botClient, chatId, message.Text, dbContext, cancellationToken, message);
                    break;

                case var cmd when cmd.StartsWith("/frontendquestions"):
                    await questionService.ListQuestionsAsync<FrontendQuestion>(botClient, chatId, dbContext, cancellationToken);
                    break;

                case var cmd when cmd.StartsWith("/givefrontendquestion"):
                    await questionService.GiveQuestionAsync<FrontendQuestion>(botClient, chatId, dbContext, cancellationToken);
                    break;
                
                case var cmd when cmd.StartsWith("/createquiz"):
                    await _quizService.CreateAndSendQuizAsync(botClient, chatId, message.Text, cancellationToken);
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
            await botClient.SendTextMessageAsync(chatId, "An error occurred while processing the command.", cancellationToken: cancellationToken);
        }
    }

    private async Task HandleStartCommand(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
    {
        await HandleCommandsCommand(botClient, chatId, cancellationToken);
    }
    
    private async Task HandleCommandsCommand(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
    {
        var commandsList = "Hi, here are my commands:\n" +
                           "/start - Start interacting with the bot\n" +
                           "/profile - Show your profile\n" +
                           "/givebackendquestion - Get a backend question\n" +
                           "/givefrontendquestion - Get a frontend question\n" +
                           "/finduser @username - Find a user by username\n" +
                           "/weather [place] - Get weather for a location\n" +
                           "/findrole [role] - Find users by role\n" +
                           "/createquiz | <question> | <correct_option_id> | <option1> | <option2> | ... - Create a quiz\n" +
                           "/commands - Show all commands\n";

        await botClient.SendTextMessageAsync(chatId, commandsList, cancellationToken: cancellationToken);
    }
    
    private async Task HandleCleanupCommandAsync(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
    {
        await botClient.SendTextMessageAsync(chatId, "Starting cleanup...", cancellationToken: cancellationToken);

        using (var scope = _serviceScopeFactory.CreateScope())
        {
            var scopedProfileService = scope.ServiceProvider.GetRequiredService<ProfileService>();
            var scopedDbContext = scope.ServiceProvider.GetRequiredService<BotDbContext>();

            await scopedProfileService.CleanUpDuplicateProfilesAsync(scopedDbContext, cancellationToken);
        }

        await botClient.SendTextMessageAsync(chatId, "Cleanup completed.", cancellationToken: cancellationToken);
        _logger.LogInformation("Cleanup command executed successfully.");
    }

    private async Task HandleFindUserCommandAsync(ITelegramBotClient botClient, long chatId, string messageText, 
        ProfileService profileService, BotDbContext dbContext, CancellationToken cancellationToken)
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
            await botClient.SendTextMessageAsync(chatId, "Invalid command format. Use: /finduser @username", cancellationToken: cancellationToken);
            _logger.LogWarning("Invalid format for /finduser command.");
        }
    }

    private async Task HandleCustomCommandAsync(ITelegramBotClient botClient, Message message, long chatId, 
        BotDbContext dbContext, ProfileService profileService, CancellationToken cancellationToken)
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
        else if (message.Text!.StartsWith("/unban"))
        {
            await profileService.HandleUnbanCommandAsync(botClient, message, chatId, dbContext, cancellationToken);
        }
        else
        {
            _logger.LogWarning($"Unknown command received: {message.Text}");
        }
    }

    public async Task HandleMessageTypeAsync(ITelegramBotClient botClient, Message message, long chatId, 
        BotDbContext dbContext, CancellationToken cancellationToken)
    {
        switch (message.Type)
        {
            case MessageType.ChatMembersAdded:
                await _profileService.WelcomeNewMembersAsync(botClient, message, chatId, cancellationToken, dbContext);
                break;
            case MessageType.ChatMemberLeft when message.LeftChatMember is not null:
                await _profileService.FarewellMemberAsync(botClient, message, chatId, cancellationToken);
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

    private async Task HandleFindRoleCommandAsync(ITelegramBotClient botClient, long chatId, string cmd, 
        ProfileService profileService, BotDbContext dbContext, CancellationToken cancellationToken)
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