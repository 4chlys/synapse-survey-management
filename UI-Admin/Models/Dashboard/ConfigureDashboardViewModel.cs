namespace SynapseEnqueteManagement.UI.Admin.Models.Dashboard;

public class ConfigureDashboardViewModel
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string[] Layout { get; set; } = [];
    public string[] AllWidgetTypes { get; set; } = [];
}
