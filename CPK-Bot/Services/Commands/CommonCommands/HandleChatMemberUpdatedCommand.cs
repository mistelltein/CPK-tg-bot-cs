using CPK_Bot.Data.Context;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace CPK_Bot.Services.Commands.CommonCommands;

public class HandleChatMemberUpdatedCommand : ICommand
{
    private readonly ILogger<HandleChatMemberUpdatedCommand> _logger;

    public HandleChatMemberUpdatedCommand(ILogger<HandleChatMemberUpdatedCommand> logger)
    {
        _logger = logger;
    }

    public async Task ExecuteAsync(ITelegramBotClient botClient, Update update, long chatId, BotDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (update.ChatMember == null)
        {
            _logger.LogError("ChatMemberUpdated is null in ExecuteAsync");
            return;
        }

        var chatMemberUpdated = update.ChatMember;
        var newStatus = chatMemberUpdated.NewChatMember.Status;
        var oldStatus = chatMemberUpdated.OldChatMember.Status;
        chatId = chatMemberUpdated.Chat.Id;
        var userName = chatMemberUpdated.NewChatMember.User.Username ?? chatMemberUpdated.NewChatMember.User.FirstName;

        _logger.LogInformation("User {UserName} status changed from {OldStatus} to {NewStatus} in chat {ChatId}", userName, oldStatus, newStatus, chatId);

        switch (newStatus)
        {
            case ChatMemberStatus.Member when oldStatus != ChatMemberStatus.Member:
                _logger.LogInformation("Sending welcome message to {UserName} in chat {ChatId}", userName, chatId);
                await botClient.SendTextMessageAsync(chatId, $"Welcome {userName}!", cancellationToken: cancellationToken);
                break;
            case ChatMemberStatus.Left or ChatMemberStatus.Kicked:
                _logger.LogInformation("Sending farewell message to {UserName} in chat {ChatId}", userName, chatId);
                await botClient.SendTextMessageAsync(chatId, $"{userName} has left the chat.", cancellationToken: cancellationToken);
                break;
            default:
                _logger.LogWarning("Unhandled status change from {OldStatus} to {NewStatus} for user {UserName} in chat {ChatId}", oldStatus, newStatus, userName, chatId);
                break;
        }
    }
}
