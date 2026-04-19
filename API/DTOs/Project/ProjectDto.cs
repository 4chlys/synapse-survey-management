namespace SynapseEnqueteManagement.API.DTOs.Project;

public class ProjectDto
{
    public int ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public string ProjectState { get; set; } = string.Empty;
    public string FormType { get; set; } = string.Empty;
    public string OrganisationName { get; set; } = string.Empty;
    public string ProjectImage { get; set; } = string.Empty;
    public string OrganisationLogoUrl { get; set; } = string.Empty;
    public string OrganisationWebsiteUrl { get; set; } = string.Empty;
    public string Introduction { get; set; } = string.Empty;
    public string AiAssistance { get; set; } = string.Empty;
    public bool RequestContactInfo { get; set; }
    public int? MinAge { get; set; }
    public int? MaxAge { get; set; }
}
