namespace SynapseEnqueteManagement.API.DTOs.Ideas;

public record IdeaPageDto
{
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int ApprovedCount { get; init; }
    public int PendingCount { get; init; }
    public int FlaggedCount { get; init; }
    public int DeclinedCount { get; init; }
    public ICollection<AdminIdeaDto> Items { get; init; } = new List<AdminIdeaDto>();
}
