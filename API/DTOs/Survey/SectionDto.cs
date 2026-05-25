namespace SynapseEnqueteManagement.API.DTOs.Survey;

public record SectionDto
{
    public int SectionId { get; init; }
    public string Name { get; init; }
    public string Description { get; init; }
}
