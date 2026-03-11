namespace SynapseEnqueteManagement.API.DTOs;

public class ProjectDto
{
    public int ProjectId { get; set; }
    public string ProjectState { get; set; } = string.Empty;
    public string OrganisationName { get; set; } = string.Empty;
    public string OrganisationLogoUrl { get; set; } = string.Empty;
    public string OrganisationWebsiteUrl { get; set; } = string.Empty;
}
