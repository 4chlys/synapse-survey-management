using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.DAL;

namespace SynapseEnqueteManagement.BL;

public class ParticipantManager(IRepository repository) : IParticipantManager
{
    public async Task<Participant> AddParticipantAsync(Questionnaire questionnaire, int projectId)
    {
        var participant = new Participant { Questionnaire = questionnaire, ProjectId = projectId };
        return await repository.CreateAsync(participant);
    }

    public async Task<Participant> GetParticipantByIdAsync(int participantId)
    {
        return await repository.ReadByIdAsync<Participant>(participantId);
    }

    public async Task<Participant> GetParticipantBySessionTokenAsync(Guid sessionToken)
    {
        return await repository.ReadSingleAsync<Participant>(
            filters: [p => p.SessionToken == sessionToken],
            includes: [p => p.Project]
        );
    }

    public async Task<bool> HasParticipantRespondedAsync(Guid sessionToken)
    {
        return await repository.ExistsAsync<Response>(r => r.Participant.SessionToken == sessionToken);
    }
}
