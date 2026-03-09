using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.DAL;

namespace SynapseEnqueteManagement.BL;

public class ParticipantManager : IParticipantManager
{
    private readonly IRepository _repository;

    public ParticipantManager(IRepository repository)
    {
        _repository = repository;
    }

    public Participant AddParticipant(Questionnaire questionnaire)
    {
        var participant = new Participant { Questionnaire = questionnaire };
        return _repository.Create(participant);
    }

    public Participant GetParticipantById(int participantId)
    {
        return _repository.ReadById<Participant>(participantId);
    }

    public Participant GetParticipantBySessionToken(Guid sessionToken)
    {
        return _repository.ReadSingle<Participant>(
            filters: [p => p.SessionToken == sessionToken]
        );
    }

    public bool HasParticipantResponded(Guid sessionToken)
    {
        var participant = _repository.ReadSingle<Participant>(
            filters: [p => p.SessionToken == sessionToken],
            includes: [p => p.Responses]
        );
        return participant?.Responses.Count > 0;
    }
}
