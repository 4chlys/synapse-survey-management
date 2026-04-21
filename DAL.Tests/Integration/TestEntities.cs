using SynapseEnqueteManagement.BL.Domain.Project.Entities;

namespace SynapseEnqueteManagement.DAL.Tests.Integration;

/// <summary>
/// Factory helpers for entity graphs that satisfy required-relationship
/// and not-null constraints of the DbContext schema.
/// </summary>
internal static class TestEntities
{
    public static Project NewProject(string name = "Test Org") => new()
    {
        OrganisationName = name,
        Questionnaire = new Questionnaire()
    };

    public static Emoji NewEmoji(string symbol = "👍", string name = "thumbsup") => new()
    {
        Symbol = symbol,
        Name = name
    };
}
