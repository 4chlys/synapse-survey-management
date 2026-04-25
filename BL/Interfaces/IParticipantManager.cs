using SynapseEnqueteManagement.BL.Domain.Project.Entities;

namespace SynapseEnqueteManagement.BL.Interfaces;

public interface IParticipantManager
{
    Task<Participant> AddParticipantAsync(Project project, CancellationToken ct = default);
    Task<Participant> GetParticipantByIdReadOnlyAsync(int participantId);
    Task<Participant> GetParticipantBySessionTokenForUpdateAsync(Guid sessionToken);
    Task<bool> HasParticipantSubmittedAsync(Guid sessionToken);
    Task MarkAsSubmittedAsync(Guid sessionToken, CancellationToken ct = default);
    Task SaveContactInfoAsync(int participantId, string email, CancellationToken ct = default);
    Task<int> CountCompletionsByProjectAsync(int projectId);
}
