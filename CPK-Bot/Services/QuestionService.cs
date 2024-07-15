using CPK_Bot.Data.Context;
using CPK_Bot.Models;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bots.Types;
using Message = Telegram.Bot.Types.Message;

namespace CPK_Bot.Services;

public class QuestionService
{
    public async Task AddBackendQuestionAsync(ITelegramBotClient botClient, long chatId, string messageText, BotDbContext dbContext, CancellationToken cancellationToken, Message message)
    {
        if (message.From?.Username != "arrogganz")
        {
            await botClient.SendTextMessageAsync(chatId, "У вас нет разрешения на добавление вопросов.", cancellationToken: cancellationToken);
            return;
        }

        var parts = messageText.Split('|');
        if (parts.Length != 3)
        {
            await botClient.SendTextMessageAsync(chatId, "Неверный формат команды. Используйте: /addbackendquestion | [вопрос] | [ответ]", cancellationToken: cancellationToken);
            return;
        }

        var question = new BackendQuestion
        {
            Question = parts[1].Trim(),
            Answer = parts[2].Trim()
        };

        dbContext.BackendQuestions.Add(question);
        await dbContext.SaveChangesAsync(cancellationToken);

        await botClient.SendTextMessageAsync(chatId, "Вопрос и ответ успешно добавлены.", cancellationToken: cancellationToken);
    }

    public async Task ListBackendQuestionsAsync(ITelegramBotClient botClient, long chatId, BotDbContext dbContext, CancellationToken cancellationToken)
    {
        var questions = await dbContext.BackendQuestions.Select(q => q.Question).ToListAsync(cancellationToken);
        if (!questions.Any())
        {
            await botClient.SendTextMessageAsync(chatId, "Вопросы не найдены.", cancellationToken: cancellationToken);
            return;
        }

        var questionList = string.Join("\n- ", questions.Prepend("Список вопросов:"));
        var formattedMessage = $"*{questionList}*";

        await botClient.SendTextMessageAsync(
            chatId,
            formattedMessage,
            parseMode: (Telegram.Bot.Types.Enums.ParseMode?)ParseMode.Markdown,
            cancellationToken: cancellationToken
        );
    }

    public async Task GiveBackendQuestionAsync(ITelegramBotClient botClient, long chatId, BotDbContext dbContext, CancellationToken cancellationToken)
    {
        var random = new Random();
        var questionsCount = await dbContext.BackendQuestions.CountAsync(cancellationToken);
        if (questionsCount == 0)
        {
            await botClient.SendTextMessageAsync(chatId, "Вопросы не найдены.", cancellationToken: cancellationToken);
            return;
        }

        var randomIndex = random.Next(questionsCount);
        var question = await dbContext.BackendQuestions.Skip(randomIndex).FirstAsync(cancellationToken);

        await botClient.SendTextMessageAsync(chatId, $"Вопрос: {question.Question}\nОтвет: {question.Answer}", cancellationToken: cancellationToken);
    }

    public async Task AddFrontendQuestionAsync(ITelegramBotClient botClient, long chatId, string messageText, BotDbContext dbContext, CancellationToken cancellationToken, Message message)
    {
        if (message.From?.Username != "arrogganz")
        {
            await botClient.SendTextMessageAsync(chatId, "У вас нет разрешения на добавление вопросов.", cancellationToken: cancellationToken);
            return;
        }

        var parts = messageText.Split('|');
        if (parts.Length != 3)
        {
            await botClient.SendTextMessageAsync(chatId, "Неверный формат команды. Используйте: /addfrontendquestion | [вопрос] | [ответ]", cancellationToken: cancellationToken);
            return;
        }

        var question = new FrontendQuestion
        {
            Question = parts[1].Trim(),
            Answer = parts[2].Trim()
        };

        dbContext.FrontendQuestions.Add(question);
        await dbContext.SaveChangesAsync(cancellationToken);

        await botClient.SendTextMessageAsync(chatId, "Вопрос и ответ успешно добавлены.", cancellationToken: cancellationToken);
    }

    public async Task ListFrontendQuestionsAsync(ITelegramBotClient botClient, long chatId, BotDbContext dbContext, CancellationToken cancellationToken)
    {
        var questions = await dbContext.FrontendQuestions.Select(q => q.Question).ToListAsync(cancellationToken);
        if (!questions.Any())
        {
            await botClient.SendTextMessageAsync(chatId, "Вопросы не найдены.", cancellationToken: cancellationToken);
            return;
        }

        var questionList = string.Join("\n- ", questions.Prepend("Список вопросов:"));
        var formattedMessage = $"*{questionList}*";

        await botClient.SendTextMessageAsync(
            chatId,
            formattedMessage,
            parseMode: ParseMode.Markdown as Telegram.Bot.Types.Enums.ParseMode?,
            cancellationToken: cancellationToken
        );
    }

    public async Task GiveFrontendQuestionAsync(ITelegramBotClient botClient, long chatId, BotDbContext dbContext, CancellationToken cancellationToken)
    {
        var random = new Random();
        var questionsCount = await dbContext.FrontendQuestions.CountAsync(cancellationToken);
        if (questionsCount == 0)
        {
            await botClient.SendTextMessageAsync(chatId, "Вопросы не найдены.", cancellationToken: cancellationToken);
            return;
        }

        var randomIndex = random.Next(questionsCount);
        var question = await dbContext.FrontendQuestions.Skip(randomIndex).FirstAsync(cancellationToken);

        await botClient.SendTextMessageAsync(chatId, $"Вопрос: {question.Question}\nОтвет: {question.Answer}", cancellationToken: cancellationToken);
    }
}
