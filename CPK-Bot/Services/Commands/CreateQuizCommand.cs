using CPK_Bot.Data.Context;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace CPK_Bot.Services.Commands;

public class CreateQuizCommand : ICommand
{
    private readonly QuizService _quizService;

    public CreateQuizCommand(QuizService quizService)
    {
        _quizService = quizService;
    }
    
    public async Task ExecuteAsync(ITelegramBotClient botClient, Message message, long chatId, BotDbContext dbContext,
        CancellationToken cancellationToken)
    {
        await _quizService.CreateAndSendQuizAsync(botClient, chatId, message.Text!, cancellationToken);
    }
}