using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.DAL;
using SynapseEnqueteManagement.BL.Interfaces;

namespace SynapseEnqueteManagement.BL.Implementation;

public class ParticipantManager(IRepository repository, IUnitOfWork uow) : IParticipantManager
{
    public async Task<Participant> AddParticipantAsync(Questionnaire questionnaire, int projectId, CancellationToken ct = default)
    {
        var trackedQuestionnaire = await repository.ReadByIdAsync<Questionnaire>(questionnaire.QuestionnaireId, tracked: true);
        var participant = new Participant
        {
            Questionnaire = trackedQuestionnaire,
            Project = await repository.ReadByIdAsync<Project>(projectId, tracked: true),
            CreatedAt = DateTime.UtcNow
        };
        var created = await repository.CreateAsync(participant);
        await uow.SaveChangesAsync(ct);
        return created;
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

    public async Task MarkAsSubmittedAsync(Guid sessionToken, CancellationToken ct = default)
    {
        var participant = await repository.ReadSingleAsync<Participant>(
            filters: [p => p.SessionToken == sessionToken],
            tracked: true);
        if (participant is null)
            throw new KeyNotFoundException("Participant not found.");
        participant.HasSubmitted = true;
        repository.Update(participant);
        await uow.SaveChangesAsync(ct);
    }

    public async Task SaveContactInfoAsync(int participantId, string email, CancellationToken ct = default)
    {
        var participant = await repository.ReadByIdAsync<Participant>(participantId, tracked: true);
        if (participant is null)
            throw new KeyNotFoundException($"Participant with id {participantId} not found.");
        participant.Email = email;
        repository.Update(participant);
        await uow.SaveChangesAsync(ct);
    }
}