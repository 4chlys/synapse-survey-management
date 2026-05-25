namespace SynapseEnqueteManagement.API.DTOs.Project;

public record ProjectShortFormLookupDto
{
    public int ProjectId { get; init; }
    public string ShortForm { get; init; }
    public string ProjectState { get; init; }
}
