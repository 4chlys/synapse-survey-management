namespace SynapseEnqueteManagement.UI.Admin.Services;

public record ActivationProblemDto(string EntityType, int? EntityId, string LanguageCode);

/// Thrown by <see cref="Project.ProjectAdminService.UpdateProjectStateAsync"/> when the API
/// returns 400 with a structured problems list (incomplete translations or missing categories).
public sealed class ProjectActivationValidationException(IReadOnlyList<ActivationProblemDto> problems)
    : Exception("Project activation blocked.")
{
    public IReadOnlyList<ActivationProblemDto> Problems { get; } = problems;
}
