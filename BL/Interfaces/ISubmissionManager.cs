using SynapseEnqueteManagement.BL.Domain.Project.Entities;

namespace SynapseEnqueteManagement.BL.Interfaces;

public interface ISubmissionManager
{
    Task SubmitAsync(int projectId, Participant participant, CancellationToken ct = default);
}
