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
        
        var botService = host.Services.GetRequiredService<IBotService>();
        botService.Start();
        
        await host.RunAsync();
    }

    private static void ConfigureServices(HostBuilderContext context, IServiceCollection services)
    {
        services.AddDbContext<BotDbContext>(options =>
            options.UseNpgsql(context.Configuration.GetConnectionString("DefaultConnection")));
                
        var token = context.Configuration["TelegramBotApiKey"];
        services.AddSingleton(new TelegramBotClient(token!));
                
        services.AddSingleton<IBotService, BotService>();
        services.AddHttpClient<IWeatherService, WeatherService>();
                
        services.AddScoped<IUpdateHandler, UpdateHandler>(); 
        services.AddScoped<ICommandHandler, CommandHandler>(); 
        services.AddScoped<IProfileService, ProfileService>(); 
        services.AddScoped<IQuestionService, QuestionService>();
        services.AddScoped<IQuizService, QuizService>();
                
        services.AddScoped<AddBackendQuestionCommand>();
        services.AddScoped<AddFrontendQuestionCommand>();
        services.AddScoped<AllRolesCommand>();
        services.AddScoped<BanCommand>();
        services.AddScoped<CleanupCommand>();
        services.AddScoped<ICommandFactory, CommandFactory>();
        services.AddScoped<CreateQuizCommand>();
        services.AddScoped<FindRoleCommand>();
        services.AddScoped<FindUserCommand>();
        services.AddScoped<GiveBackendQuestionCommand>();
        services.AddScoped<GiveFrontendQuestionCommand>();
        services.AddScoped<HandleBotCommand>();
        services.AddScoped<HandleCommandsCommand>();
        services.AddScoped<HandleChatMemberUpdatedCommand>();
        services.AddScoped<HandleMessageTypeCommand>();
        services.AddScoped<HandleOtherCommand>();
        services.AddScoped<ListBackendQuestionsCommand>();
        services.AddScoped<ListFrontendQuestionsCommand>();
        services.AddScoped<ProfileCommand>();
        services.AddScoped<RateCommand>();
        services.AddScoped<SetRoleCommand>();
        services.AddScoped<StartCommand>();
        services.AddScoped<UnbanCommand>();
        services.AddScoped<WeatherCommand>();
        services.AddScoped<SendMessageCommand>();
    }
}