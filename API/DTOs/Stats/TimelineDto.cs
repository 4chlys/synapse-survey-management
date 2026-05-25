namespace SynapseEnqueteManagement.API.DTOs.Stats;

public record TimelineDto
{
    public ICollection<string> Dates { get; init; } = new List<string>();
    public ICollection<int> Starts { get; init; } = new List<int>();
    public ICollection<int> Completions { get; init; } = new List<int>();
}
