namespace SynapseEnqueteManagement.API.DTOs.Stats;

public class ModerationQueueDto
{
    public int Approved { get; set; }
    public int Declined { get; set; }
    public int Flagged { get; set; }
    public int Unflagged { get; set; }
}
