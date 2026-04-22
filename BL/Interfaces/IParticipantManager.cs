using SynapseEnqueteManagement.BL.Domain.Project.Entities;

namespace SynapseEnqueteManagement.BL.Interfaces;

public interface IParticipantManager
{
    Task<Participant> AddParticipantAsync(Questionnaire questionnaire, int projectId, CancellationToken ct = default);
    Task<Participant> GetParticipantByIdAsync(int participantId);
    Task<Participant> GetParticipantBySessionTokenAsync(Guid sessionToken);
    Task<bool> HasParticipantSubmittedAsync(Guid sessionToken);
    Task MarkAsSubmittedAsync(Guid sessionToken, CancellationToken ct = default);
    Task SaveContactInfoAsync(int participantId, string email, CancellationToken ct = default);
}