namespace SynapseEnqueteManagement.UI.Admin.Models.Dashboard;

public class WidgetSlotInput
{
    public string WidgetType { get; set; } = string.Empty;
    public int? QuestionId { get; set; }
}

public class ConfigureDashboardViewModel
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string[] Layout { get; set; } = [];
    public WidgetSlotInput[] Slots { get; set; } = [];
    public string[] AllWidgetTypes { get; set; } = [];
}
