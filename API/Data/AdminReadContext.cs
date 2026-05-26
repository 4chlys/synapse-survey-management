using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SynapseEnqueteManagement.API.Data;

public class AdminReadContext(DbContextOptions<AdminReadContext> options) : DbContext(options)
{
    public DbSet<PlatformSettingsReadModel> PlatformSettings { get; set; }
    public DbSet<SystemAiSettingsReadModel> SystemAiSettings { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PlatformSettingsReadModel>(e =>
        {
            e.HasKey(s => s.Id);
            e.ToTable("PlatformSettings");
        });

        modelBuilder.Entity<SystemAiSettingsReadModel>(e =>
        {
            e.HasKey(s => s.Id);
            e.ToTable("SystemAiSettings");
        });
    }
}

[Table("PlatformSettings")]
public sealed class PlatformSettingsReadModel
{
    public int Id { get; set; }
    public int ProjectPlatformId { get; set; }
    public string SelectedAiModel { get; set; }
    public string PromptModeration { get; set; }
    public string PromptCommentModeration { get; set; }
    public string PromptRewrite { get; set; }
    public string PromptImageModeration { get; set; }
    public string PromptIdeaSupport { get; set; }
    public string PromptCommentSupport { get; set; }
    public string PromptQuestionSupport { get; set; }
    public string PromptIdeaSummary { get; set; }
    public int IdeaSummaryMaxIdeas { get; set; } = 200;
}

[Table("SystemAiSettings")]
public sealed class SystemAiSettingsReadModel
{
    public int Id { get; set; }
    public string AllowedModelIds { get; set; }
}
