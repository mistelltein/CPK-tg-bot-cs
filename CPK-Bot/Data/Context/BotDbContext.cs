using CPK_Bot.Models;
using CPK_Bot.Models.Configurations;
using Microsoft.EntityFrameworkCore;

namespace CPK_Bot.Data.Context;

public class BotDbContext : DbContext
{
    public BotDbContext(DbContextOptions<BotDbContext> options) : base(options) { }
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