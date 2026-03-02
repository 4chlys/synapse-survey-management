using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace SynapseEnqueteManagement.DAL;

public class SynapseEnqueteContext : IdentityDbContext
{
    public SynapseEnqueteContext(DbContextOptions options) : base(options)
    {
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