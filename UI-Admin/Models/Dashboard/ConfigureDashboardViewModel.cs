namespace SynapseEnqueteManagement.UI.Admin.Models.Dashboard;

public record ConfigureDashboardViewModel
{
    public int ProjectId { get; init; }
    public string ProjectName { get; init; } = string.Empty;
    public string[] Layout { get; init; } = [];
    public string[] AllWidgetTypes { get; init; } = [];
}
