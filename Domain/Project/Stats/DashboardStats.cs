namespace SynapseEnqueteManagement.BL.Domain.Project.Stats;

public record CategoryMetric(int CategoryId, string Name, int IdeaCount, int TotalReactions);
public record IdeaMetric(int IdeaId, string Description, int ReactionCount, int CommentCount);
public record DashboardMetrics(List<CategoryMetric> TopCategories, List<IdeaMetric> TopIdeas);
