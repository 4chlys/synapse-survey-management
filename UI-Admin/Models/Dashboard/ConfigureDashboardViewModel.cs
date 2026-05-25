namespace SynapseEnqueteManagement.UI.Admin.Models.Dashboard;

public class WidgetSlotInput
{
    public string WidgetType { get; set; } = string.Empty;
    public int? QuestionId { get; set; }
}

public record ConfigureDashboardViewModel
{
    public int ProjectId { get; init; }
    public string ProjectName { get; init; } = string.Empty;
    public string[] Layout { get; init; } = [];
    public WidgetSlotInput[] Slots { get; init; } = [];
    public string[] AllWidgetTypes { get; init; } = [];
}
