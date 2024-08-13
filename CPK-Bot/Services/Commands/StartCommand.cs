using CPK_Bot.Data.Context;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace CPK_Bot.Services.Commands;

public class StartCommand : ICommand
{
    public async Task ExecuteAsync(ITelegramBotClient botClient, Message message, long chatId, BotDbContext dbContext, 
        CancellationToken cancellationToken)
    {
        const string commandsList = "Hi, here are my commands:\n" +
                                    "/start - Start interacting with the bot\n" +
                                    "/profile - Show your profile\n" +
                                    "/givebackendquestion - Get a backend question\n" +
                                    "/givefrontendquestion - Get a frontend question\n" +
                                    "/finduser @username - Find a user by username\n" +
                                    "/weather [place] - Get weather for a location\n" +
                                    "/findrole [role] - Find users by role\n" +
                                    "/createquiz | <question> | <correct_option_id> | <option1> | <option2> | ... - Create a quiz\n" +
                                    "/showallroles - show available roles" +
                                    "/commands - Show all commands\n";

        await botClient.SendTextMessageAsync(chatId, commandsList, cancellationToken: cancellationToken);
    }
}