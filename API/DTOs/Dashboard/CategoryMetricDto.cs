namespace SynapseEnqueteManagement.API.DTOs.Dashboard;

public class CategoryMetricDto
{
    public int CategoryId { get; set; }
    public string Name { get; set; }
    public int IdeaCount { get; set; }
    public int TotalReactions { get; set; }
}
