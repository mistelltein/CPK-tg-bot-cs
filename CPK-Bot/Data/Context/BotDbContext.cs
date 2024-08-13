using CPK_Bot.Entities;
using CPK_Bot.Entities.Configurations;
using Microsoft.EntityFrameworkCore;

namespace CPK_Bot.Data.Context;

public class BotDbContext(DbContextOptions<BotDbContext> options) : DbContext(options)
{
    public DbSet<Profile> Profiles { get; set; }
    public DbSet<FrontendQuestion> FrontendQuestions { get; set; }
    public DbSet<BackendQuestion> BackendQuestions { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new ProfileConfigurations());
        modelBuilder.ApplyConfiguration(new FrontendQuestionConfigurations());
        modelBuilder.ApplyConfiguration(new BackendQuestionConfigurations());
    }
}