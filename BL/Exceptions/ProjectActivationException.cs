using SynapseEnqueteManagement.BL.Interfaces;

namespace SynapseEnqueteManagement.BL.Exceptions;

public class ProjectActivationException(IReadOnlyList<ActivationProblem> problems) : Exception("Project activation blocked by incomplete translations or missing categories.")
{
    public IReadOnlyList<ActivationProblem> Problems { get; } = problems;
}
