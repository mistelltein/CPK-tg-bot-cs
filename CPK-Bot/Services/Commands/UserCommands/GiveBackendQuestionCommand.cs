using CPK_Bot.Data.Context;
using CPK_Bot.Entities;
using CPK_Bot.Services.Commands.CommonCommands;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace CPK_Bot.Services.Commands.UserCommands;

public class GiveBackendQuestionCommand : ICommand
{
    private readonly IQuestionService _questionService;

    public GiveBackendQuestionCommand(IQuestionService questionService)
    {
        _questionService = questionService;
    }
    
    public async Task ExecuteAsync(ITelegramBotClient botClient, Update update, long chatId, BotDbContext dbContext,
        CancellationToken cancellationToken)
    {
        await _questionService.GiveQuestionAsync<BackendQuestion>(botClient, chatId, dbContext, cancellationToken);
    }
}