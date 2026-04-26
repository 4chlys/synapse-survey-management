namespace SynapseEnqueteManagement.UI.Admin.Data;

public class UserWidgetPreference
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int ProjectId { get; set; }
    public int Position { get; set; }
    public string WidgetType { get; set; } = string.Empty;
}
