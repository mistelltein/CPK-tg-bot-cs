using CPK_Bot.Services.Commands.AdminCommands;
using CPK_Bot.Services.Commands.UserCommands;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace CPK_Bot.Services.Commands.CommonCommands;

public interface ICommandFactory
{
    ICommand GetCommand(Update update);
}

public class CommandFactory : ICommandFactory
{
    private readonly IServiceProvider _serviceProvider;

    public CommandFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }
    
    public ICommand GetCommand(Update update)
    {
        if (update.Message is not null)
        {
            var commandText = update.Message.Text?.ToLower();
            
            if (!string.IsNullOrEmpty(commandText))
            {
                return commandText switch
                {
                    _ when commandText.StartsWith("/start") => _serviceProvider.GetRequiredService<StartCommand>(),
                    _ when commandText.StartsWith("/commands") => _serviceProvider.GetRequiredService<HandleCommandsCommand>(),
                    _ when commandText.StartsWith("/profile") => _serviceProvider.GetRequiredService<ProfileCommand>(),
                    _ when commandText.StartsWith("/addbackendquestion") => _serviceProvider.GetRequiredService<AddBackendQuestionCommand>(),
                    _ when commandText.StartsWith("/backendquestions") => _serviceProvider.GetRequiredService<ListBackendQuestionsCommand>(),
                    _ when commandText.StartsWith("/givebackendquestion") => _serviceProvider.GetRequiredService<GiveBackendQuestionCommand>(),
                    _ when commandText.StartsWith("/addfrontendquestion") => _serviceProvider.GetRequiredService<AddFrontendQuestionCommand>(),
                    _ when commandText.StartsWith("/frontendquestions") => _serviceProvider.GetRequiredService<ListFrontendQuestionsCommand>(),
                    _ when commandText.StartsWith("/givefrontendquestion") => _serviceProvider.GetRequiredService<GiveFrontendQuestionCommand>(),
                    _ when commandText.StartsWith("/createquiz") => _serviceProvider.GetRequiredService<CreateQuizCommand>(),
                    _ when commandText.StartsWith("/rate") => _serviceProvider.GetRequiredService<RateCommand>(),
                    _ when commandText.StartsWith("/setrole") => _serviceProvider.GetRequiredService<SetRoleCommand>(),
                    _ when commandText.StartsWith("/ban") => _serviceProvider.GetRequiredService<BanCommand>(),
                    _ when commandText.StartsWith("/unban") => _serviceProvider.GetRequiredService<UnbanCommand>(),
                    _ when commandText.StartsWith("/finduser") => _serviceProvider.GetRequiredService<FindUserCommand>(),
                    _ when commandText.StartsWith("/weather") => _serviceProvider.GetRequiredService<WeatherCommand>(),
                    _ when commandText.StartsWith("/findrole") => _serviceProvider.GetRequiredService<FindRoleCommand>(),
                    _ when commandText.StartsWith("/showallroles") => _serviceProvider.GetRequiredService<AllRolesCommand>(),
                    _ when commandText.StartsWith("/cleanup") => _serviceProvider.GetRequiredService<CleanupCommand>(),
                    _ when commandText.StartsWith("/sendmessage") => _serviceProvider.GetRequiredService<SendMessageCommand>(),
                    _ when commandText.Equals("бот", StringComparison.CurrentCultureIgnoreCase) => _serviceProvider.GetRequiredService<HandleBotCommand>(),
                    _ => _serviceProvider.GetRequiredService<HandleOtherCommand>()
                };
            }
        }
        else if (update.ChatMember is not null) 
        {
            return _serviceProvider.GetRequiredService<HandleChatMemberUpdatedCommand>();
        }

        return update.Message!.Type switch
        {
            MessageType.ChatMembersAdded or MessageType.ChatMemberLeft => _serviceProvider.GetRequiredService<HandleMessageTypeCommand>(),
            _ => _serviceProvider.GetRequiredService<HandleOtherCommand>()
        };
    }
}