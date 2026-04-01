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
            Project = await repository.ReadByIdAsync<Project>(projectId, tracked: true),
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
            includes: [p => p.Project],
            tracked: true
        );
    }

    public async Task<bool> HasParticipantSubmittedAsync(Guid sessionToken)
    {
        var participant = await repository.ReadSingleAsync<Participant>(
            filters: [p => p.SessionToken == sessionToken]);
        return participant?.HasSubmitted ?? false;
    }

    public async Task MarkAsSubmittedAsync(Guid sessionToken)
    {
        var participant = await repository.ReadSingleAsync<Participant>(
            filters: [p => p.SessionToken == sessionToken],
            tracked: true);
        if (participant is null)
            throw new KeyNotFoundException("Participant not found.");
        participant.HasSubmitted = true;
        await repository.UpdateAsync(participant);
    }

    public async Task SaveContactInfoAsync(int participantId, string email, string phoneNumber)
    {
        var participant = await repository.ReadByIdAsync<Participant>(participantId, tracked: true);
        if (participant is null)
            throw new KeyNotFoundException($"Participant with id {participantId} not found.");
        participant.Email = email;
        participant.PhoneNumber = phoneNumber;
        await repository.UpdateAsync(participant);
    }
}