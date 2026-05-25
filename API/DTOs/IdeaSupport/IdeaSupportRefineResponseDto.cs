namespace SynapseEnqueteManagement.API.DTOs.IdeaSupport;

public record IdeaSupportRefineResponseDto
{
    /// <summary>Null when no meaningful refinement could be produced.</summary>
    public string RefinedText { get; init; }
}