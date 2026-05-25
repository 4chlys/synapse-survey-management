namespace SynapseEnqueteManagement.UI.User.Models;

public record ProjectViewModel
{
    public int ProjectId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string ProjectState { get; init; } = string.Empty;
    public string FormType { get; init; } = "VerticalScroll";
    public string OrganisationName { get; init; } = string.Empty;
    public string OrganisationLogoUrl { get; init; } = string.Empty;
    public string OrganisationWebsiteUrl { get; init; } = string.Empty;
    public string ProjectImage { get; init; } = string.Empty;
    public bool IsAiGeneratedImage { get; init; }
    public string Introduction { get; init; } = string.Empty;
    public bool RequestContactInfo { get; init; }
    public bool ExtraIdeasReplaceExisting { get; init; }
    public int MaxExtraIdeaRequests { get; init; } = 3;
    public string DefaultLanguageCode { get; init; } = "nl";
    public bool AllowCrossLanguageIdeas { get; init; }
    public ICollection<string> SupportedLanguageCodes { get; init; } = new List<string>();

    public string Theme { get; init; } = "Default";
    public string PrimaryColor { get; init; }
    public string CustomCss { get; init; }
}
