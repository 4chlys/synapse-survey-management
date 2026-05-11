namespace SynapseEnqueteManagement.UI.User.Models;

public class ProjectViewModel
{
    public int ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ProjectState { get; set; } = string.Empty;
    public string FormType { get; set; } = "VerticalScroll";
    public string OrganisationName { get; set; } = string.Empty;
    public string OrganisationLogoUrl { get; set; } = string.Empty;
    public string OrganisationWebsiteUrl { get; set; } = string.Empty;
    public string Introduction { get; set; } = string.Empty;
    public bool RequestContactInfo { get; set; }
    public bool ExtraIdeasReplaceExisting { get; set; }
    public int MaxExtraIdeaRequests { get; set; } = 3;
    public string DefaultLanguageCode { get; set; } = "nl";
    public bool AllowCrossLanguageIdeas { get; set; }
    public ICollection<string> SupportedLanguageCodes { get; set; } = new List<string>();

    public string Theme { get; set; } = "Default";
    public string PrimaryColor { get; set; }
    public string CustomCss { get; set; }
}
