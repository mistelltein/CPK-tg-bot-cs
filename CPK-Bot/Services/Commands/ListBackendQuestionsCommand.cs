using CPK_Bot.Data.Context;
using CPK_Bot.Entities;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace CPK_Bot.Services.Commands;

public class ListBackendQuestionsCommand : ICommand
{
    private readonly QuestionService _questionService;

    public ListBackendQuestionsCommand(QuestionService questionService)
    {
        _questionService = questionService;
    }
    
    public async Task ExecuteAsync(ITelegramBotClient botClient, Message message, long chatId, BotDbContext dbContext,
        CancellationToken cancellationToken)
    {
        await _questionService.ListQuestionsAsync<BackendQuestion>(botClient, chatId, dbContext, cancellationToken);
    }
}