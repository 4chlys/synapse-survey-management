using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SynapseEnqueteManagement.BL.Domain.Project.Entities;

namespace SynapseEnqueteManagement.DAL;

public class SynapseEnqueteContext : IdentityDbContext
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

    public SynapseEnqueteContext(DbContextOptions options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
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