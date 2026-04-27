namespace SynapseEnqueteManagement.API.DTOs.Stats;

public class TimelineDto
{
    public List<string> Dates { get; set; } = new();
    public List<int> Starts { get; set; } = new();
    public List<int> Completions { get; set; } = new();
}
