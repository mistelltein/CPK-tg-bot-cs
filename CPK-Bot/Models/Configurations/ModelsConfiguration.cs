using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CPK_Bot.Models.Configurations;

public class ProfileConfigurations : IEntityTypeConfiguration<Profile>
{
    public void Configure(EntityTypeBuilder<Profile> builder)
    {
        builder.HasKey(b => b.Id);

        builder.HasIndex(b => b.Username).IsUnique();
        builder.Property(b => b.Username).IsRequired(false);
        
        builder.HasIndex(b => b.Rating);
        builder.Property(b => b.Rating).IsRequired();
            
        builder.HasIndex(b => b.Role);
        builder.Property(b => b.Role).IsRequired();
        
        builder.Property(b => b.FirstName).HasMaxLength(100).IsRequired(false);
    }
}

public class BackendQuestionConfigurations : IEntityTypeConfiguration<BackendQuestion>
{
    public void Configure(EntityTypeBuilder<BackendQuestion> builder)
    {
        builder.HasKey(bq => bq.QuestionId);

        builder.Property(bq => bq.QuestionText)
            .HasColumnName("Question") 
            .HasMaxLength(1000)
            .IsRequired(false);

        builder.Property(bq => bq.Answer)
            .HasMaxLength(10000)
            .IsRequired(false);
    }
}

public class FrontendQuestionConfigurations : IEntityTypeConfiguration<FrontendQuestion>
{
    public void Configure(EntityTypeBuilder<FrontendQuestion> builder)
    {
        builder.HasKey(fq => fq.QuestionId);

        builder.Property(fq => fq.QuestionText)
            .HasColumnName("Question") 
            .HasMaxLength(1000)
            .IsRequired(false);

        builder.Property(fq => fq.Answer)
            .HasMaxLength(10000)
            .IsRequired(false);
    }
}