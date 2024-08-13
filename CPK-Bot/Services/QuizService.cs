using Microsoft.Extensions.Logging;
using Telegram.Bot;
using PollType = Telegram.Bot.Types.Enums.PollType;

namespace CPK_Bot.Services;

public interface IQuizService
{
    Task CreateAndSendQuizAsync(ITelegramBotClient botClient, long chatId, string messageText,
        CancellationToken cancellationToken);
}

public class QuizService : IQuizService
{
    private readonly ILogger<QuizService> _logger;

    public QuizService(ILogger<QuizService> logger)
    {
        _logger = logger;
    }
    
    public async Task CreateAndSendQuizAsync(ITelegramBotClient botClient, long chatId, string messageText, CancellationToken cancellationToken)
    {
        var parts = messageText.Split('|');
        if (parts.Length < 4)
        {
            await botClient.SendTextMessageAsync(chatId, "Invalid command format. Use: /createquiz | <question> | <correct_option_id> | <option1> | <option2> | ...", cancellationToken: cancellationToken);
            return;
        }

        var question = parts[1].Trim();
        if (!int.TryParse(parts[2].Trim(), out var correctOptionId) || correctOptionId < 0 || correctOptionId >= parts.Length - 3)
        {
            await botClient.SendTextMessageAsync(chatId, "Invalid correct option ID.", cancellationToken: cancellationToken);
            return;
        }

        var options = parts.Skip(3).Select(option => option.Trim()).ToArray();

        try
        {
            await botClient.SendPollAsync(
                chatId: chatId,
                question: question,
                options: options,
                isAnonymous: false,
                type: PollType.Quiz,
                correctOptionId: correctOptionId,
                cancellationToken: cancellationToken
            );
            _logger.LogInformation("Quiz sent successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending quiz.");
            await botClient.SendTextMessageAsync(chatId, "An error occurred while sending the quiz.", cancellationToken: cancellationToken);
        }
    }
}