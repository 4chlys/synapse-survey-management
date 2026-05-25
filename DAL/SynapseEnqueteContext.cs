using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using SynapseEnqueteManagement.BL.Domain.Ai.Entities;
using SynapseEnqueteManagement.BL.Domain.I18n.Entities;
using SynapseEnqueteManagement.BL.Domain.Platform.Entities;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Pgvector;
using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;

namespace SynapseEnqueteManagement.DAL;

public class SynapseEnqueteContext(DbContextOptions<SynapseEnqueteContext> options) : DbContext(options)
{
    public DbSet<Language> Languages { get; set; }
    public DbSet<Project> Projects { get; set; }
    public DbSet<ProjectTranslation> ProjectTranslations { get; set; }
    public DbSet<IntroductionTranslation> IntroductionTranslations { get; set; }
    public DbSet<CategoryTranslation> CategoryTranslations { get; set; }
    public DbSet<ProjectPlatform> ProjectPlatforms { get; set; }
    public DbSet<Questionnaire> Questionnaires { get; set; }
    public DbSet<Question> Questions { get; set; }
    public DbSet<Response> Responses { get; set; }
    public DbSet<Participant> Participants { get; set; }
    public DbSet<Idea> Ideas { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Introduction> Introductions { get; set; }
    public DbSet<Section> Sections { get; set; }
    public DbSet<SectionTranslation> SectionTranslations { get; set; }
    public DbSet<QuestionTranslation> QuestionTranslations { get; set; }
    public DbSet<EndingAnswerTranslation> EndingAnswerTranslations { get; set; }
    public DbSet<Emoji> Emojis { get; set; }
    public DbSet<IconComment> IconComments { get; set; }
    public DbSet<TextComment> TextComments { get; set; }
    public DbSet<Comment> Comments { get; set; }
    public DbSet<EndingAnswer> EndingAnswers { get; set; }
    public DbSet<AiUsageEvent> AiUsageEvents { get; set; }
    public DbSet<IdeaSummaryJob> IdeaSummaryJobs { get; set; }


    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasPostgresExtension("vector");

        // Language
        builder.Entity<Language>()
            .HasKey(l => l.Code);

        // Project
        builder.Entity<Project>()
            .HasIndex(p => p.ShortForm)
            .IsUnique();

        builder.Entity<Project>(e =>
        {
            e.HasOne(p => p.ProjectPlatform)
                .WithMany()
                .HasForeignKey(p => p.ProjectPlatformId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(p => p.Questionnaire)
                .WithOne(q => q.Project)
                .HasForeignKey<Questionnaire>("ProjectId")
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(p => p.Introduction)
                .WithOne(i => i.Project)
                .HasForeignKey<Introduction>("ProjectId")
                .OnDelete(DeleteBehavior.Cascade);

            e.HasMany(p => p.Categories)
                .WithOne(c => c.Project)
                .HasForeignKey("ProjectId")
                .OnDelete(DeleteBehavior.Cascade);

            // Participants are also reachable via Questionnaire; ProjectId FK
            // lets us query all project participants directly.
            e.HasMany(p => p.Participants)
                .WithOne(p => p.Project)
                .HasForeignKey("ProjectId")
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(p => p.DefaultLanguage)
                .WithMany()
                .HasForeignKey("DefaultLanguageCode")
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            e.HasMany(p => p.SupportedLanguages)
                .WithMany()
                .UsingEntity(j => j.ToTable("ProjectLanguage"));

            e.HasMany(p => p.Translations)
                .WithOne(t => t.Project)
                .HasForeignKey("ProjectId")
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ProjectTranslation
        builder.Entity<ProjectTranslation>(e =>
        {
            e.Property<int>("ProjectId");
            e.Property<string>("LanguageCode").HasMaxLength(10);
            e.HasKey("ProjectId", "LanguageCode");

            e.HasOne(t => t.Language)
                .WithMany()
                .HasForeignKey("LanguageCode")
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Questionnaire
        builder.Entity<Questionnaire>(e =>
        {
            e.HasMany(q => q.Questions)
                .WithOne(q => q.Questionnaire)
                .HasForeignKey("QuestionnaireId")
                .OnDelete(DeleteBehavior.Cascade);

            e.HasMany(q => q.Sections)
                .WithOne(s => s.Questionnaire)
                .HasForeignKey("QuestionnaireId")
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Question
        builder.Entity<Question>(e =>
        {
            e.HasIndex("QuestionnaireId");

            e.HasOne(q => q.Section)
                .WithMany(s => s.Questions)
                .HasForeignKey("SectionId")
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasOne(q => q.ParentQuestion)
                .WithMany(q => q.Children)
                .HasForeignKey("ParentQuestionId")
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex("ParentQuestionId");

            e.HasMany(q => q.Translations)
                .WithOne(t => t.Question)
                .HasForeignKey("QuestionId")
                .OnDelete(DeleteBehavior.Cascade);
        });

        // QuestionTranslation
        builder.Entity<QuestionTranslation>(e =>
        {
            e.Property<int>("QuestionId");
            e.Property<string>("LanguageCode").HasMaxLength(10);
            e.HasKey("QuestionId", "LanguageCode");

            e.HasOne(t => t.Language)
                .WithMany()
                .HasForeignKey("LanguageCode")
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            e.Property(t => t.AnswerChoices)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                    v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions)null)
                         ?? new List<string>()
                )
                .HasColumnType("TEXT")
                .Metadata.SetValueComparer(new ValueComparer<ICollection<string>>(
                    (c1, c2) => c1.SequenceEqual(c2),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    c => c.ToList()
                ));
        });

        // Section translations
        builder.Entity<Section>(e =>
        {
            e.HasMany(s => s.Translations)
                .WithOne(t => t.Section)
                .HasForeignKey("SectionId")
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<SectionTranslation>(e =>
        {
            e.Property<int>("SectionId");
            e.Property<string>("LanguageCode").HasMaxLength(10);
            e.HasKey("SectionId", "LanguageCode");

            e.HasOne(t => t.Language)
                .WithMany()
                .HasForeignKey("LanguageCode")
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);
        });

        // EndingAnswer — rows only exist for answers that terminate the survey.
        builder.Entity<EndingAnswer>(e =>
        {
            e.HasOne(ea => ea.Question)
                .WithMany(q => q.EndingAnswers)
                .HasForeignKey("QuestionId")
                .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex("QuestionId");

            e.HasMany(ea => ea.Translations)
                .WithOne(t => t.EndingAnswer)
                .HasForeignKey("EndingAnswerId")
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<EndingAnswerTranslation>(e =>
        {
            e.Property<int>("EndingAnswerId");
            e.Property<string>("LanguageCode").HasMaxLength(10);
            e.HasKey("EndingAnswerId", "LanguageCode");

            e.HasOne(t => t.Language)
                .WithMany()
                .HasForeignKey("LanguageCode")
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Response
        builder.Entity<Response>(e =>
        {
            e.HasOne(r => r.Question)
                .WithMany(q => q.Responses)
                .HasForeignKey("QuestionId")
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(r => r.Participant)
                .WithMany(p => p.Responses)
                .HasForeignKey("ParticipantId")
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(r => r.Language)
                .WithMany()
                .HasForeignKey("LanguageCode")
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            // Supports "which participants answered this question?" queries.
            e.HasIndex("QuestionId", "ParticipantId");
            e.HasIndex("ParticipantId");
        });

        // Participant
        builder.Entity<Participant>(e =>
        {
            // SessionToken is the lookup key for every user request.
            e.HasIndex(p => p.SessionToken).IsUnique();
            e.HasIndex("ProjectId");

            e.HasOne(p => p.Language)
                .WithMany()
                .HasForeignKey("LanguageCode")
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            e.HasMany(p => p.Comments)
                .WithOne(c => c.Participant)
                .HasForeignKey("ParticipantId")
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Introduction
        builder.Entity<Introduction>(e =>
        {
            e.HasMany(i => i.Translations)
                .WithOne(t => t.Introduction)
                .HasForeignKey("IntroductionId")
                .OnDelete(DeleteBehavior.Cascade);
        });

        // IntroductionTranslation
        builder.Entity<IntroductionTranslation>(e =>
        {
            e.Property<int>("IntroductionId");
            e.Property<string>("LanguageCode").HasMaxLength(10);
            e.HasKey("IntroductionId", "LanguageCode");

            e.HasOne(t => t.Language)
                .WithMany()
                .HasForeignKey("LanguageCode")
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Category
        builder.Entity<Category>(e =>
        {
            e.HasMany(c => c.Ideas)
                .WithOne(i => i.Category)
                .HasForeignKey("CategoryId")
                .OnDelete(DeleteBehavior.Cascade);

            e.HasMany(c => c.Translations)
                .WithOne(t => t.Category)
                .HasForeignKey("CategoryId")
                .OnDelete(DeleteBehavior.Cascade);
        });

        // CategoryTranslation
        builder.Entity<CategoryTranslation>(e =>
        {
            e.Property<int>("CategoryId");
            e.Property<string>("LanguageCode").HasMaxLength(10);
            e.HasKey("CategoryId", "LanguageCode");

            e.HasOne(t => t.Language)
                .WithMany()
                .HasForeignKey("LanguageCode")
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Idea
        builder.Entity<Idea>(e =>
        {
            // Supports filtering by category and moderation status.
            e.HasIndex("CategoryId");
            e.HasIndex(i => i.Status);

            e.HasOne(i => i.Participant)
                .WithMany(p => p.Ideas)
                .HasForeignKey("IdeaParticipantId")
                .OnDelete(DeleteBehavior.Cascade);

            e.HasMany(i => i.Comments)
                .WithOne(c => c.Idea)
                .HasForeignKey("IdeaId")
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(i => i.Language)
                .WithMany()
                .HasForeignKey("LanguageCode")
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            var embeddingComparer = new ValueComparer<float[]>(
                (a, b) => (a == null && b == null) || (a != null && b != null && a.SequenceEqual(b)),
                a => a == null ? 0 : a.Aggregate(0, (acc, v) => HashCode.Combine(acc, v.GetHashCode())),
                a => a == null ? null : (float[])a.Clone());

            if (Database.IsNpgsql())
            {
                // Native pgvector column — Pgvector wraps float[] via Vector internally.
                // Dimensions must match the embedding model (gemini-embedding-001 = 3072).
                e.Property(i => i.Embedding)
                    .HasColumnType("vector(3072)")
                    .HasConversion(
                        new ValueConverter<float[], Vector>(
                            v => v == null ? null : new Vector(v),
                            v => v == null ? null : v.Memory.ToArray()))
                    .Metadata.SetValueComparer(embeddingComparer);
            }
            else
            {
                // SQLite (dev): store as JSON text. No vector index, in-memory sort only.
                e.Property(i => i.Embedding)
                    .HasConversion(
                        new ValueConverter<float[], string>(
                            v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                            v => JsonSerializer.Deserialize<float[]>(v, (JsonSerializerOptions)null)))
                    .HasColumnType("TEXT")
                    .Metadata.SetValueComparer(embeddingComparer);
            }
        });

        // AiUsageEvent — per-call cost/token log for AI services.
        builder.Entity<AiUsageEvent>(e =>
        {
            e.Property<int>("ProjectId");

            e.HasOne(u => u.Project)
                .WithMany()
                .HasForeignKey("ProjectId")
                .OnDelete(DeleteBehavior.Cascade);

            // Supports filtering recent events by project (dashboard polling).
            e.HasIndex("ProjectId", nameof(AiUsageEvent.CreatedAt));
        });

        // IdeaSummaryJob
        builder.Entity<IdeaSummaryJob>(e =>
        {
            e.HasKey(j => j.IdeaSummaryJobId);
            if (Database.IsNpgsql())
                e.Property(j => j.IdeaSummaryJobId).HasDefaultValueSql("gen_random_uuid()");
            else
                e.Property(j => j.IdeaSummaryJobId).ValueGeneratedOnAdd();
            e.Property(j => j.Status).HasConversion<string>();
            e.Property(j => j.ExportFormat).HasConversion<string>();
            e.HasIndex(j => new { j.ProjectId, j.Status });
        });

        // Project-Emoji many-to-many (implicit join table)
        builder.Entity<Project>()
            .HasMany(p => p.Emojis)
            .WithMany()
            .UsingEntity("ProjectEmoji");

        // Comment
        builder.Entity<Comment>(e =>
        {
            e.HasDiscriminator<string>("CommentType")
                .HasValue<TextComment>("TextComment")
                .HasValue<IconComment>("IconComment");

            e.HasOne(c => c.Language)
                .WithMany()
                .HasForeignKey("LanguageCode")
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // IconComment
        builder.Entity<IconComment>(e =>
        {
            e.HasOne(ic => ic.Emoji)
                .WithMany()
                .HasForeignKey("EmojiId")
                .OnDelete(DeleteBehavior.Restrict);

            // Optional: an IconComment can be a reaction on a TextComment.
            // Cascade so reactions are removed when their parent TextComment is deleted.
            e.HasOne(ic => ic.TextComment)
                .WithMany(tc => tc.EmojiReactions)
                .HasForeignKey("TextCommentId")
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    public bool CreateDatabase(bool deleteIfExists)
    {
        if (deleteIfExists)
        {
            try
            {
                // Close all connections before deleting
                Database.CloseConnection();

                // Delete the database
                Database.EnsureDeleted();
            }
            catch (IOException ex)
            {
                throw new InvalidOperationException(
                    "Cannot delete the database. Please close any applications accessing the database file (e.g., DB Browser for SQLite).",
                    ex
                );
            }
        }

        // Create the database and return whether it was newly created
        bool isCreated = Database.EnsureCreated();
        return isCreated;
    }
}