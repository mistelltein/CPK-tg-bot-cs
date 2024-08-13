using CPK_Bot.Data.Context;
using CPK_Bot.Entities;
using CPK_Bot.Services.Commands.CommonCommands;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace CPK_Bot.Services.Commands.AdminCommands;

public class AddFrontendQuestionCommand : ICommand
{
    private readonly IQuestionService _questionService;

    public AddFrontendQuestionCommand(IQuestionService questionService)
    {
        _questionService = questionService;
    }
    
    public async Task ExecuteAsync(ITelegramBotClient botClient, Message message, long chatId, BotDbContext dbContext,
        CancellationToken cancellationToken)
    {
        await _questionService.AddQuestionAsync<FrontendQuestion>(botClient, chatId, message.Text!, dbContext, cancellationToken, message);
    }
}