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

    public async Task<Participant> AddParticipantAsync(Questionnaire questionnaire)
    {
        var participant = new Participant { Questionnaire = questionnaire };
        return await _repository.CreateAsync(participant);
    }

    public async Task<Participant> GetParticipantByIdAsync(int participantId)
    {
        return await _repository.ReadByIdAsync<Participant>(participantId);
    }

    public async Task<Participant> GetParticipantBySessionTokenAsync(Guid sessionToken)
    {
        return await _repository.ReadSingleAsync<Participant>(
            filters: [p => p.SessionToken == sessionToken]
        );
    }

    public async Task<bool> HasParticipantRespondedAsync(Guid sessionToken)
    {
        var participant = await _repository.ReadSingleAsync<Participant>(
            filters: [p => p.SessionToken == sessionToken],
            includes: [p => p.Responses]
        );
        return participant?.Responses.Count > 0;
    }
}
