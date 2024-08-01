using CPK_Bot.Data.Context;
using CPK_Bot.Helpers;
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

    public async Task AddQuestionAsync<T>(ITelegramBotClient botClient, long chatId, string messageText, 
        BotDbContext dbContext, CancellationToken cancellationToken, Message message) where T : Question, new()
    {
        if (message.From?.Username != "arrogganz")
        {
            await botClient.SendTextMessageAsync(chatId, "You do not have permission to add questions.", cancellationToken: cancellationToken);
            return;
        }

        var parts = messageText.Split('|');
        if (parts.Length != 3)
        {
            await botClient.SendTextMessageAsync(chatId, $"Invalid command format. Use: /add{typeof(T).Name.ToLower()} | [question] | [answer]", cancellationToken: cancellationToken);
            return;
        }

        try
        {
            var question = new T
            {
                QuestionText = parts[1].Trim(),
                Answer = parts[2].Trim()
            };

            dbContext.Set<T>().Add(question);
            await dbContext.SaveChangesAsync(cancellationToken);

            await botClient.SendTextMessageAsync(chatId, "Question and answer added successfully.", cancellationToken: cancellationToken);
            _logger.LogInformation($"{typeof(T).Name} added successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error adding {typeof(T).Name}.");
            await botClient.SendTextMessageAsync(chatId, "An error occurred while adding the question.", cancellationToken: cancellationToken);
        }
    }

    public async Task ListQuestionsAsync<T>(ITelegramBotClient botClient, long chatId, 
        BotDbContext dbContext, CancellationToken cancellationToken) where T : Question
    {
        try
        {
            var questions = await dbContext.Set<T>().Select(q => q.QuestionText).ToListAsync(cancellationToken);
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
            _logger.LogInformation($"Listed {typeof(T).Name} questions successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error listing {typeof(T).Name} questions.");
            await botClient.SendTextMessageAsync(chatId, "An error occurred while fetching the list of questions.", cancellationToken: cancellationToken);
        }
    }

    public async Task GiveQuestionAsync<T>(ITelegramBotClient botClient, long chatId, BotDbContext dbContext, 
        CancellationToken cancellationToken) where T : Question
    {
        try
        {
            var random = new Random();
            var questionsCount = await dbContext.Set<T>().CountAsync(cancellationToken);
            if (questionsCount == 0)
            {
                await botClient.SendTextMessageAsync(chatId, "No questions found.", cancellationToken: cancellationToken);
                return;
            }

            var randomIndex = random.Next(questionsCount);
            var question = await dbContext.Set<T>().Skip(randomIndex).FirstAsync(cancellationToken);
            
            var escapedQuestion = TelegramMarkdownHelper.EscapeMarkdownV2(question.QuestionText!);
            var escapedAnswer = TelegramMarkdownHelper.EscapeMarkdownV2(question.Answer!);

            var messageText = $"*Question:* {escapedQuestion}\n*Answer:* ||{escapedAnswer}||";

            await botClient.SendTextMessageAsync(
                chatId,
                messageText,
                parseMode: Telegram.Bot.Types.Enums.ParseMode.MarkdownV2,
                cancellationToken: cancellationToken
            );
            _logger.LogInformation($"Provided {typeof(T).Name} question successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error providing {typeof(T).Name} question.");
            await botClient.SendTextMessageAsync(chatId, "An error occurred while fetching the question.", cancellationToken: cancellationToken);
        }
    }
}