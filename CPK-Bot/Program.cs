using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using CPK_Bot.Data.Context;
using CPK_Bot.Services;
using CPK_Bot.Services.Commands.AdminCommands;
using CPK_Bot.Services.Commands.CommonCommands;
using CPK_Bot.Services.Commands.UserCommands;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;

namespace CPK_Bot;

internal static class Program
{
    private static async Task Main()
    {
        var host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((config) =>
            {
                config.SetBasePath(Directory.GetCurrentDirectory());
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            })
            .ConfigureServices(ConfigureServices)
            .Build();
        
        var botService = host.Services.GetRequiredService<BotService>();
        botService.Start();
        
        await host.RunAsync();
    }

    private static void ConfigureServices(HostBuilderContext context, IServiceCollection services)
    {
        services.AddDbContext<BotDbContext>(options =>
            options.UseNpgsql(context.Configuration.GetConnectionString("DefaultConnection")));
                
        var token = context.Configuration["TelegramBotApiKey"];
        services.AddSingleton(new TelegramBotClient(token!));
                
        services.AddSingleton<BotService>();
        services.AddHttpClient<WeatherService>();
                
        services.AddTransient<UpdateHandler>(); 
        services.AddTransient<CommandHandler>(); 
        services.AddTransient<ProfileService>(); 
        services.AddTransient<QuestionService>();
        services.AddTransient<QuizService>();
                
        services.AddTransient<AddBackendQuestionCommand>();
        services.AddTransient<AddFrontendQuestionCommand>();
        services.AddTransient<AllRolesCommand>();
        services.AddTransient<BanCommand>();
        services.AddTransient<CleanupCommand>();
        services.AddTransient<CommandFactory>();
        services.AddTransient<CreateQuizCommand>();
        services.AddTransient<FindRoleCommand>();
        services.AddTransient<FindUserCommand>();
        services.AddTransient<GiveBackendQuestionCommand>();
        services.AddTransient<GiveFrontendQuestionCommand>();
        services.AddTransient<HandleBotCommand>();
        services.AddTransient<HandleCommandsCommand>();
        services.AddTransient<HandleMessageTypeCommand>();
        services.AddTransient<HandleOtherCommand>();
        services.AddTransient<ListBackendQuestionsCommand>();
        services.AddTransient<ListFrontendQuestionsCommand>();
        services.AddTransient<ProfileCommand>();
        services.AddTransient<RateCommand>();
        services.AddTransient<SetRoleCommand>();
        services.AddTransient<StartCommand>();
        services.AddTransient<UnbanCommand>();
        services.AddTransient<WeatherCommand>();
    }
}