namespace SynapseEnqueteManagement.API.DTOs.Ideas;

public class IdeaPageDto
{
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int ApprovedCount { get; set; }
    public int PendingCount { get; set; }
    public int FlaggedCount { get; set; }
    public List<IdeaDto> Items { get; set; } = new();
}
