using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using CPK_Bot.Data.Context;
using CPK_Bot.Services;
using Microsoft.EntityFrameworkCore;


namespace CPK_Bot;

internal static class Program
{
    private static Task Main()
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

                services.AddSingleton<BotService>();
            })
            .Build();

        var botService = host.Services.GetRequiredService<BotService>();
        botService.Start();
        return Task.CompletedTask;
    }
}