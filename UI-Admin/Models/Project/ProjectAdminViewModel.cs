namespace SynapseEnqueteManagement.UI.Admin.Models.Project;

public record ProjectAdminViewModel
{
    public int ProjectId { get; init; }
    public string Name { get; init; }
    public string ShortForm { get; init; }
    public string Topic { get; init; }
    public string ProjectImage { get; init; }
    public bool IsAiGeneratedImage { get; init; }
    public string ProjectState { get; init; }
    public string FormType { get; init; }
    public string OrganisationName { get; init; }
    public string OrganisationLogoUrl { get; init; }
    public int PlatformId { get; init; }
    public string Introduction { get; init; }
    public string AiAssistance { get; init; } = "ProActive";
    public bool RequestContactInfo { get; init; }
    public int IdeaPageSize { get; init; } = 5;
    public bool ExtraIdeasReplaceExisting { get; init; }
    public int MaxExtraIdeaRequests { get; init; } = 3;
    public int? MinAge { get; init; }
    public int? MaxAge { get; init; }
    public bool AllowCrossLanguageIdeas { get; init; }
    public string DefaultLanguageCode { get; init; }
    public ICollection<string> SupportedLanguageCodes { get; init; } = new List<string>();

    public string Theme { get; init; } = "Default";
    public string PrimaryColor { get; init; }
    public string CustomCss { get; init; }
}
