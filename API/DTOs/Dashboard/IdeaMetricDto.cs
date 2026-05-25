namespace SynapseEnqueteManagement.API.DTOs.Dashboard;

public record IdeaMetricDto
{
    public int IdeaId { get; init; }
    public string Description { get; init; }
    public int ReactionCount { get; init; }
    public int CommentCount { get; init; }
}
