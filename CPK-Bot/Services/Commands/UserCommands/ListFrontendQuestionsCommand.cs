using CPK_Bot.Data.Context;
using CPK_Bot.Entities;
using CPK_Bot.Services.Commands.CommonCommands;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace CPK_Bot.Services.Commands.UserCommands;

public class ListFrontendQuestionsCommand : ICommand
{
    private readonly IQuestionService _questionService;

    public ListFrontendQuestionsCommand(IQuestionService questionService)
    {
        _questionService = questionService;
    }
    
    public async Task ExecuteAsync(ITelegramBotClient botClient, Message message, long chatId, BotDbContext dbContext,
        CancellationToken cancellationToken)
    {
        await _questionService.ListQuestionsAsync<FrontendQuestion>(botClient, chatId, dbContext, cancellationToken);
    }
}