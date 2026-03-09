using SynapseEnqueteManagement.BL.Domain.Project.Entities;

namespace SynapseEnqueteManagement.BL;

public interface IParticipantManager
{
    Participant AddParticipant(Questionnaire questionnaire);
    Participant GetParticipantById(int participantId);
    Participant GetParticipantBySessionToken(Guid sessionToken);
    bool HasParticipantResponded(Guid sessionToken);
}
