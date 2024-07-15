using CPK_Bot.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace CPK_Bot.Services;

public class CommandHandler(IServiceProvider serviceProvider)
{
    public async Task HandleTextMessageAsync(ITelegramBotClient botClient, Message message, long chatId, BotDbContext dbContext, CancellationToken cancellationToken)
    {
        var profileService = serviceProvider.GetRequiredService<ProfileService>();
        var questionService = serviceProvider.GetRequiredService<QuestionService>();

        if (message.From != null)
        {
            await profileService.RegisterUserAsync(message.From, "Member", dbContext, cancellationToken);
        }

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            Console.WriteLine($"Error saving changes: {ex.Message}");
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

        switch (message.Text!.ToLower())
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
                await profileService.ShowProfileAsync(botClient, chatId, message.From!.Username!, dbContext, cancellationToken);
                break;

            case var command when command.StartsWith("/addbackendquestion"):
                await questionService.AddBackendQuestionAsync(botClient, chatId, message.Text, dbContext, cancellationToken, message);
                break;

            case var command when command.StartsWith("/backendquestions"):
                await questionService.ListBackendQuestionsAsync(botClient, chatId, dbContext, cancellationToken);
                break;

            case var command when command.StartsWith("/givebackendquestion"):
                await questionService.GiveBackendQuestionAsync(botClient, chatId, dbContext, cancellationToken);
                break;

            case var command when command.StartsWith("/addfrontendquestion"):
                await questionService.AddFrontendQuestionAsync(botClient, chatId, message.Text, dbContext, cancellationToken, message);
                break;

            case var command when command.StartsWith("/frontendquestions"):
                await questionService.ListFrontendQuestionsAsync(botClient, chatId, dbContext, cancellationToken);
                break;

            case var command when command.StartsWith("/givefrontendquestion"):
                await questionService.GiveFrontendQuestionAsync(botClient, chatId, dbContext, cancellationToken);
                break;

            case "/cleanup":
                await botClient.SendTextMessageAsync(chatId, "Starting cleanup...", cancellationToken: cancellationToken);
                using (var scope = serviceProvider.CreateScope())
                {
                    dbContext = scope.ServiceProvider.GetRequiredService<BotDbContext>();
                    await profileService.CleanUpDuplicateProfilesAsync(dbContext, cancellationToken);
                }
                await botClient.SendTextMessageAsync(chatId, "Cleanup completed.", cancellationToken: cancellationToken);
                break;

            case var command when command.StartsWith("/finduser"):
                var parts = message.Text.Split(' ');
                if (parts.Length == 2)
                {
                    var username = parts[1].TrimStart('@');
                    await profileService.ShowProfileByUsernameAsync(botClient, chatId, username, dbContext, cancellationToken);
                }
                else
                {
                    await botClient.SendTextMessageAsync(chatId, "Неверный формат команды. Используйте: /finduser @username", cancellationToken: cancellationToken);
                }
                break;

            default:
                if (message.Text!.StartsWith("/rate"))
                {
                    await profileService.HandleRateCommandAsync(botClient, message, chatId, dbContext, cancellationToken);
                }
                else if (message.Text!.StartsWith("/setrole"))
                {
                    await profileService.HandleSetRoleCommandAsync(botClient, message, chatId, dbContext, cancellationToken);
                }
                break;
        }
    }

    public async Task HandleMessageTypeAsync(ITelegramBotClient botClient, Message message, long chatId, BotDbContext dbContext, CancellationToken cancellationToken)
    {
        var profileService = serviceProvider.GetRequiredService<ProfileService>();

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
}
