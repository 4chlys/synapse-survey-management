namespace SynapseEnqueteManagement.API.DTOs.Project;

public class ProjectDto
{
    public int ProjectId { get; set; }
    public string ProjectState { get; set; } = string.Empty;
    public string FormType { get; set; } = string.Empty;
    public string OrganisationName { get; set; } = string.Empty;
    public string OrganisationLogoUrl { get; set; } = string.Empty;
    public string OrganisationWebsiteUrl { get; set; } = string.Empty;
    public string Introduction { get; set; } = string.Empty;
}
