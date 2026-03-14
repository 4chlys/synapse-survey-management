using SynapseEnqueteManagement.BL.Domain.Project.Entities;

namespace SynapseEnqueteManagement.BL;

public interface IParticipantManager
{
    Task<Participant> AddParticipantAsync(Questionnaire questionnaire, int projectId);
    Task<Participant> GetParticipantByIdAsync(int participantId);
    Task<Participant> GetParticipantBySessionTokenAsync(Guid sessionToken);
    Task<bool> HasParticipantRespondedAsync(Guid sessionToken);
}