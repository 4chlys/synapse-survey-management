namespace SynapseEnqueteManagement.UI.Admin.Models.Project;

public class ProjectAdminViewModel
{
    public int ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string ProjectState { get; set; } = string.Empty;
}
