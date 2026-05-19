namespace SynapseEnqueteManagement.API.DTOs.Stats;

public class TimelineDto
{
    public ICollection<string> Dates { get; set; } = new List<string>();
    public ICollection<int> Starts { get; set; } = new List<int>();
    public ICollection<int> Completions { get; set; } = new List<int>();
}
