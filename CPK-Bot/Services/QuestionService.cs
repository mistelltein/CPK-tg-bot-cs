using CPK_Bot.Data.Context;
using CPK_Bot.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Message = Telegram.Bot.Types.Message;

namespace CPK_Bot.Services;

public class QuestionService
{
    private readonly ILogger<QuestionService> _logger;

    public QuestionService(ILogger<QuestionService> logger)
    {
        _logger = logger;
    }

    public async Task AddBackendQuestionAsync(ITelegramBotClient botClient, long chatId, string messageText, BotDbContext dbContext, CancellationToken cancellationToken, Message message)
    {
        if (message.From?.Username != "arrogganz")
        {
            await botClient.SendTextMessageAsync(chatId, "You do not have permission to add questions.", cancellationToken: cancellationToken);
            return;
        }

        var parts = messageText.Split('|');
        if (parts.Length != 3)
        {
            await botClient.SendTextMessageAsync(chatId, "Invalid command format. Use: /addbackendquestion | [question] | [answer]", cancellationToken: cancellationToken);
            return;
        }

        try
        {
            var question = new BackendQuestion
            {
                Question = parts[1].Trim(),
                Answer = parts[2].Trim()
            };

            dbContext.BackendQuestions.Add(question);
            await dbContext.SaveChangesAsync(cancellationToken);

            await botClient.SendTextMessageAsync(chatId, "Question and answer added successfully.", cancellationToken: cancellationToken);
            _logger.LogInformation("Backend question added successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding backend question.");
            await botClient.SendTextMessageAsync(chatId, "An error occurred while adding the question.", cancellationToken: cancellationToken);
        }
    }

    public async Task ListBackendQuestionsAsync(ITelegramBotClient botClient, long chatId, BotDbContext dbContext, CancellationToken cancellationToken)
    {
        try
        {
            var questions = await dbContext.BackendQuestions.Select(q => q.Question).ToListAsync(cancellationToken);
            if (!questions.Any())
            {
                await botClient.SendTextMessageAsync(chatId, "No questions found.", cancellationToken: cancellationToken);
                return;
            }

            var questionList = string.Join("\n- ", questions.Prepend("List of questions:"));
            var formattedMessage = $"*{questionList}*";

            await botClient.SendTextMessageAsync(
                chatId,
                formattedMessage,
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
                cancellationToken: cancellationToken
            );
            _logger.LogInformation("Listed backend questions successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing backend questions.");
            await botClient.SendTextMessageAsync(chatId, "An error occurred while fetching the list of questions.", cancellationToken: cancellationToken);
        }
    }

    public async Task GiveBackendQuestionAsync(ITelegramBotClient botClient, long chatId, BotDbContext dbContext, CancellationToken cancellationToken)
    {
        try
        {
            var random = new Random();
            var questionsCount = await dbContext.BackendQuestions.CountAsync(cancellationToken);
            if (questionsCount == 0)
            {
                await botClient.SendTextMessageAsync(chatId, "No questions found.", cancellationToken: cancellationToken);
                return;
            }

            var randomIndex = random.Next(questionsCount);
            var question = await dbContext.BackendQuestions.Skip(randomIndex).FirstAsync(cancellationToken);

            await botClient.SendTextMessageAsync(chatId, $"Question: {question.Question}\nAnswer: {question.Answer}", cancellationToken: cancellationToken);
            _logger.LogInformation("Provided backend question successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error providing backend question.");
            await botClient.SendTextMessageAsync(chatId, "An error occurred while fetching the question.", cancellationToken: cancellationToken);
        }
    }

    public async Task AddFrontendQuestionAsync(ITelegramBotClient botClient, long chatId, string messageText, BotDbContext dbContext, CancellationToken cancellationToken, Message message)
    {
        if (message.From?.Username != "arrogganz")
        {
            await botClient.SendTextMessageAsync(chatId, "You do not have permission to add questions.", cancellationToken: cancellationToken);
            return;
        }

        var parts = messageText.Split('|');
        if (parts.Length != 3)
        {
            await botClient.SendTextMessageAsync(chatId, "Invalid command format. Use: /addfrontendquestion | [question] | [answer]", cancellationToken: cancellationToken);
            return;
        }

        try
        {
            var question = new FrontendQuestion
            {
                Question = parts[1].Trim(),
                Answer = parts[2].Trim()
            };

            dbContext.FrontendQuestions.Add(question);
            await dbContext.SaveChangesAsync(cancellationToken);

            await botClient.SendTextMessageAsync(chatId, "Question and answer added successfully.", cancellationToken: cancellationToken);
            _logger.LogInformation("Frontend question added successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding frontend question.");
            await botClient.SendTextMessageAsync(chatId, "An error occurred while adding the question.", cancellationToken: cancellationToken);
        }
    }

    public async Task ListFrontendQuestionsAsync(ITelegramBotClient botClient, long chatId, BotDbContext dbContext, CancellationToken cancellationToken)
    {
        try
        {
            var questions = await dbContext.FrontendQuestions.Select(q => q.Question).ToListAsync(cancellationToken);
            if (questions.Count == 0)
            {
                await botClient.SendTextMessageAsync(chatId, "No questions found.", cancellationToken: cancellationToken);
                return;
            }

            var questionList = string.Join("\n- ", questions.Prepend("List of questions:"));
            var formattedMessage = $"*{questionList}*";

            await botClient.SendTextMessageAsync(
                chatId,
                formattedMessage,
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
                cancellationToken: cancellationToken
            );
            _logger.LogInformation("Listed frontend questions successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing frontend questions.");
            await botClient.SendTextMessageAsync(chatId, "An error occurred while fetching the list of questions.", cancellationToken: cancellationToken);
        }
    }

    public async Task GiveFrontendQuestionAsync(ITelegramBotClient botClient, long chatId, BotDbContext dbContext, CancellationToken cancellationToken)
    {
        try
        {
            var random = new Random();
            var questionsCount = await dbContext.FrontendQuestions.CountAsync(cancellationToken);
            if (questionsCount == 0)
            {
                await botClient.SendTextMessageAsync(chatId, "No questions found.", cancellationToken: cancellationToken);
                return;
            }

            var randomIndex = random.Next(questionsCount);
            var question = await dbContext.FrontendQuestions.Skip(randomIndex).FirstAsync(cancellationToken);

            await botClient.SendTextMessageAsync(chatId, $"Question: {question.Question}\nAnswer: {question.Answer}", cancellationToken: cancellationToken);
            _logger.LogInformation("Provided frontend question successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error providing frontend question.");
            await botClient.SendTextMessageAsync(chatId, "An error occurred while fetching the question.", cancellationToken: cancellationToken);
        }
    }
}