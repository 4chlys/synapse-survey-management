using SynapseEnqueteManagement.BL.Domain.I18n.Entities;
using SynapseEnqueteManagement.BL.Domain.Platform.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;

namespace SynapseEnqueteManagement.DAL.Tests.Integration;

/// <summary>
/// Factory helpers for entity graphs that satisfy required-relationship
/// and not-null constraints of the DbContext schema.
/// </summary>
internal static class TestEntities
{
    public static Language NewLanguage(string code = "nl", string displayName = "Nederlands") =>
        new() { Code = code, DisplayName = displayName, IsActive = true };

    public static Project NewProject(Language language, string name = "Test Project")
    {
        var project = new Project
        {
            ShortForm = "test-project",
            Questionnaire = new Questionnaire(),
            ProjectPlatform = new ProjectPlatform { Name = "Test Platform", CreatedAt = DateTime.UtcNow },
            DefaultLanguage = language,
            SupportedLanguages = new List<Language> { language },
        };
        project.Translations.Add(new ProjectTranslation { Language = language, Name = name });
        return project;
    }

    public static Emoji NewEmoji(string symbol = "👍", string name = "thumbsup") => new()
    {
        Symbol = symbol,
        Name = name
    };

    public static Category NewCategory(Project project) => new() { Project = project };

    public static Participant NewParticipant(Project project, Language language) => new()
    {
        Project       = project,
        Questionnaire = project.Questionnaire,
        Language      = language,
        CreatedAt     = DateTime.UtcNow,
    };

    public static Idea NewIdea(
        Category category,
        Participant participant,
        ModerationStatus status  = ModerationStatus.Approved,
        Language language        = null) => new()
    {
        Category    = category,
        Participant = participant,
        Description = "Test idea",
        Status      = status,
        Language    = language,
        CreatedAt   = DateTime.UtcNow,
    };
}
