namespace SynapseEnqueteManagement.API.DTOs.Stats;

public record ModerationQueueDto
{
    public int Approved { get; init; }
    public int Declined { get; init; }
    public int Flagged { get; init; }
    public int Unflagged { get; init; }
}
