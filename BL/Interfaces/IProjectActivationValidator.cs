using SynapseEnqueteManagement.BL.Domain.Project.Entities;

namespace SynapseEnqueteManagement.BL.Interfaces;

public record ActivationProblem(string EntityType, int? EntityId, string LanguageCode);

public interface IProjectActivationValidator
{
    IReadOnlyList<ActivationProblem> Validate(Project project);
}
