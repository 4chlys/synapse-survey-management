using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using SynapseEnqueteManagement.BL.Domain.Project.Entities;

namespace SynapseEnqueteManagement.DAL;

public class SynapseEnqueteContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<Project> Projects { get; set; }
    public DbSet<Questionnaire> Questionnaires { get; set; }
    public DbSet<Question> Questions { get; set; }
    public DbSet<Response> Responses { get; set; }
    public DbSet<Participant> Participants { get; set; }
    public DbSet<Idea> Ideas { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Introduction> Introductions { get; set; }
    public DbSet<Section> Sections { get; set; }
    public DbSet<Emoji> Emojis { get; set; }
    public DbSet<IconComment> IconComments { get; set; }
    public DbSet<TextComment> TextComments { get; set; }
    public DbSet<Comment> Comments { get; set; }
    public DbSet<BranchRule> BranchRules { get; set; }


    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Project
        builder.Entity<Project>(e =>
        {
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
        });

        // Questionnaire
        builder.Entity<Questionnaire>(e =>
        {
            e.HasMany(q => q.Questions)
                .WithOne(q => q.Questionnaire)
                .HasForeignKey("QuestionnaireId")
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Question
        builder.Entity<Question>(e =>
        {
            // Serialize the list of answer choices as a JSON array in a TEXT column.
            e.Property(q => q.AnswerChoices)
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

            e.HasIndex("QuestionnaireId");

            e.HasOne(q => q.Section)
                .WithMany(s => s.Questions)
                .HasForeignKey("SectionId")
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);
        });
        // BranchRule — two FKs to Question
        builder.Entity<BranchRule>()
            .HasOne(br => br.SourceQuestion)
            .WithMany(q => q.BranchRules)
            .HasForeignKey("SourceQuestionId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<BranchRule>(e =>
        {
            e.HasOne(br => br.TargetQuestion)
                .WithMany(q => q.IncomingBranches)
                .HasForeignKey("TargetQuestionId")
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasIndex("TargetQuestionId");
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

            e.HasMany(p => p.Comments)
                .WithOne(c => c.Participant)
                .HasForeignKey("ParticipantId")
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Category
        builder.Entity<Category>(e =>
        {
            e.HasMany(c => c.Ideas)
                .WithOne(i => i.Category)
                .HasForeignKey("CategoryId")
                .OnDelete(DeleteBehavior.Cascade);
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
        });

        // IconComment
        builder.Entity<IconComment>(e =>
        {
            e.HasOne(ic => ic.Emoji)
                .WithMany()
                .HasForeignKey(ic => ic.EmojiId)
                .OnDelete(DeleteBehavior.Restrict);

            // Optional: an IconComment can be a reaction on a TextComment.
            e.HasOne(ic => ic.TextComment)
                .WithMany(tc => tc.EmojiReactions)
                .HasForeignKey("TextCommentId")
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);
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