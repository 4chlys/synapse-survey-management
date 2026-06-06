namespace SynapseEnqueteManagement.API.DTOs.Project;

public record ProjectShortFormSuggestionDto
{
    public string Suggestion { get; init; }
    public bool Available { get; init; }
}
