using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using CPK_Bot.Data.Context;
using CPK_Bot.Services.Commands.CommonCommands;
using Microsoft.Extensions.Configuration;

namespace CPK_Bot.Services.Commands.AdminCommands;

public class SendMessageCommand : ICommand
{
    private readonly ILogger<SendMessageCommand> _logger;
    private readonly long _adminChatId; 

    public SendMessageCommand(ILogger<SendMessageCommand> logger, IConfiguration configuration)
    {
        _logger = logger;
        _adminChatId = long.Parse(configuration["AdminChatId"]!); 
    }

    public async Task ExecuteAsync(ITelegramBotClient botClient, Message message, long chatId, BotDbContext dbContext, 
        CancellationToken cancellationToken)
    {
        if (message.From?.Username != "arrogganz") 
        {
            await botClient.SendTextMessageAsync(chatId, "You do not have permission to use this command.", 
                cancellationToken: cancellationToken);
            return;
        }

        var messageParts = message.Text?.Split(' ', 2);

        if (messageParts == null || messageParts.Length < 2)
        {
            await botClient.SendTextMessageAsync(chatId, "Invalid command format. Use: /sendmessage [your message]", 
                cancellationToken: cancellationToken);
            return;
        }

        var adminMessage = messageParts[1];

        try
        {
            await botClient.SendTextMessageAsync(_adminChatId, $"{adminMessage}", parseMode: ParseMode.Markdown, 
                cancellationToken: cancellationToken);
            await botClient.SendTextMessageAsync(chatId, "The message was successfully delivered", 
                cancellationToken: cancellationToken);

            _logger.LogInformation("Message from {Username} was sent to admin: {Message}", message.From.Username, adminMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message to admin.");
            await botClient.SendTextMessageAsync(chatId, "An error occurred while sending the message.", 
                cancellationToken: cancellationToken);
        }
    }
}