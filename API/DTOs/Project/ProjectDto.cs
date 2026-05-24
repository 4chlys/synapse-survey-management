namespace SynapseEnqueteManagement.API.DTOs.Project;

public record ProjectDto
{
    public int ProjectId { get; init; }
    public int PlatformId { get; init; }
    public string Name { get; init; }
    public string ShortForm { get; init; }
    public string Topic { get; init; }
    public string ProjectState { get; init; }
    public string FormType { get; init; }
    public string OrganisationName { get; init; }
    public string ProjectImage { get; init; }
    public bool IsAiGeneratedImage { get; init; }
    public string OrganisationLogoUrl { get; init; }
    public string OrganisationWebsiteUrl { get; init; }
    public string Introduction { get; init; }
    public string AiAssistance { get; init; }
    public bool RequestContactInfo { get; init; }
    public int IdeaPageSize { get; init; }
    public bool ExtraIdeasReplaceExisting { get; init; }
    public int MaxExtraIdeaRequests { get; init; }
    public bool AllowCrossLanguageIdeas { get; init; }
    public int? MinAge { get; init; }
    public int? MaxAge { get; init; }

    public string LanguageCode { get; init; }
    public string DefaultLanguageCode { get; init; }
    public ICollection<string> SupportedLanguageCodes { get; init; } = new List<string>();

    public int LastWizardStep { get; init; }

    public string Theme { get; init; }
    public string PrimaryColor { get; init; }
    public string CustomCss { get; init; }
}
