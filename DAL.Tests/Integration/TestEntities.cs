using SynapseEnqueteManagement.BL.Domain.Platform.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Entities;

namespace SynapseEnqueteManagement.DAL.Tests.Integration;

/// <summary>
/// Factory helpers for entity graphs that satisfy required-relationship
/// and not-null constraints of the DbContext schema.
/// </summary>
internal static class TestEntities
{
    public static Project NewProject(string name = "Test Project") => new()
    {
        Name = name,
        Questionnaire = new Questionnaire(),
        ProjectPlatform = new ProjectPlatform { Name = "Test Platform", CreatedAt = DateTime.UtcNow }
    };

    public static Emoji NewEmoji(string symbol = "👍", string name = "thumbsup") => new()
    {
        Symbol = symbol,
        Name = name
    };
}
