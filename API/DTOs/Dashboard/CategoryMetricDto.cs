namespace SynapseEnqueteManagement.API.DTOs.Dashboard;

public record CategoryMetricDto
{
    public int CategoryId { get; init; }
    public string Name { get; init; }
    public int IdeaCount { get; init; }
    public int TotalReactions { get; init; }
}
