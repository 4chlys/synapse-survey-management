namespace SynapseEnqueteManagement.UI.Admin.Models.Project;

public class ProjectAdminViewModel
{
    public int ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ProjectImage { get; set; } = string.Empty;
    public string ProjectState { get; set; } = string.Empty;
    public string OrganisationName { get; set; } = string.Empty;
    public string OrganisationLogoUrl { get; set; } = string.Empty;
}
