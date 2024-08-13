using CPK_Bot.Data.Context;
using CPK_Bot.Entities;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace CPK_Bot.Services.Commands;

public class AddBackendQuestionCommand : ICommand
{
    private readonly QuestionService _questionService;

    public AddBackendQuestionCommand(QuestionService questionService)
    {
        _questionService = questionService;
    }
    
    public async Task ExecuteAsync(ITelegramBotClient botClient, Message message, long chatId, BotDbContext dbContext,
        CancellationToken cancellationToken)
    {
        await _questionService.AddQuestionAsync<BackendQuestion>(botClient, chatId, message.Text!, dbContext, cancellationToken, message);
    }
}