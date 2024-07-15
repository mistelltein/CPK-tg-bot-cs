using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using CPK_Bot.Data.Context;
using CPK_Bot.Services;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;


namespace CPK_Bot;

internal static class Program
{
    private static async Task Main()
    {
        var host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((context, config) =>
            {
                config.SetBasePath(Directory.GetCurrentDirectory());
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            })
            .ConfigureServices((context, services) =>
            {
                services.AddDbContext<BotDbContext>(options =>
                    options.UseSqlite(context.Configuration.GetConnectionString("DefaultConnection")));
                
                var token = context.Configuration["TelegramBotApiKey"];
                services.AddSingleton(new TelegramBotClient(token!));
                
                services.AddSingleton<BotService>();
                services.AddTransient<UpdateHandler>(); 
                services.AddTransient<CommandHandler>(); 
                services.AddTransient<ProfileService>(); 
                services.AddTransient<QuestionService>();
            })
            .Build();
        
        var botService = host.Services.GetRequiredService<BotService>();
        botService.Start();
        
        await host.RunAsync();
    }
}