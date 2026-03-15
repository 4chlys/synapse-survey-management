using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.DAL;
using SynapseEnqueteManagement.BL.Interfaces;

namespace SynapseEnqueteManagement.BL.Implementation;

public class ParticipantManager(IRepository repository) : IParticipantManager
{
    public async Task<Participant> AddParticipantAsync(Questionnaire questionnaire, int projectId)
    {
        var participant = new Participant
        {
            Questionnaire = questionnaire,
            Project = await repository.ReadByIdAsync<Project>(projectId),
            CreatedAt = DateTime.UtcNow
        };
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

    public async Task SaveContactInfoAsync(int participantId, string email, string phoneNumber)
    {
        var participant = await repository.ReadByIdAsync<Participant>(participantId);
        if (participant is null) return;
        participant.Email = email;
        participant.PhoneNumber = phoneNumber;
        await repository.UpdateAsync(participant);
    }
}