namespace SynapseEnqueteManagement.API.DTOs.Project;

public class ProjectDto
{
    public int ProjectId { get; set; }
    public int PlatformId { get; set; }
    public string Name { get; set; }
    public string ShortForm { get; set; }
    public string Topic { get; set; }
    public string ProjectState { get; set; }
    public string FormType { get; set; }
    public string OrganisationName { get; set; }
    public string ProjectImage { get; set; }
    public string OrganisationLogoUrl { get; set; }
    public string OrganisationWebsiteUrl { get; set; }
    public string Introduction { get; set; }
    public string AiAssistance { get; set; }
    public bool RequestContactInfo { get; set; }
    public int IdeaPageSize { get; set; }
    public bool ExtraIdeasReplaceExisting { get; set; }
    public int MaxExtraIdeaRequests { get; set; }
    public int? MinAge { get; set; }
    public int? MaxAge { get; set; }

    public string LanguageCode { get; set; }
    public string DefaultLanguageCode { get; set; }
    public ICollection<string> SupportedLanguageCodes { get; set; } = new List<string>();
}
