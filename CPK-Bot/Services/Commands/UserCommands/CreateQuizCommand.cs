using CPK_Bot.Data.Context;
using CPK_Bot.Services.Commands.CommonCommands;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace CPK_Bot.Services.Commands.UserCommands;

public class CreateQuizCommand : ICommand
{
    private readonly IQuizService _quizService;

    public CreateQuizCommand(IQuizService quizService)
    {
        _quizService = quizService;
    }
    
    public async Task ExecuteAsync(ITelegramBotClient botClient, Message message, long chatId, BotDbContext dbContext,
        CancellationToken cancellationToken)
    {
        await _quizService.CreateAndSendQuizAsync(botClient, chatId, message.Text!, cancellationToken);
    }
}