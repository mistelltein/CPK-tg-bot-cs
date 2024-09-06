using CPK_Bot.Data.Context;
using CPK_Bot.Entities;
using CPK_Bot.Services.Commands.CommonCommands;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace CPK_Bot.Services.Commands.AdminCommands;

public class AddBackendQuestionCommand : ICommand
{
    private readonly IQuestionService _questionService;

    public AddBackendQuestionCommand(IQuestionService questionService)
    {
        _questionService = questionService;
    }
    
    public async Task ExecuteAsync(ITelegramBotClient botClient, Update update, long chatId, BotDbContext dbContext,
        CancellationToken cancellationToken)
    {
        await _questionService.AddQuestionAsync<BackendQuestion>(botClient, chatId, update.Message!.Text!, dbContext, 
            cancellationToken, update.Message);
    }
}