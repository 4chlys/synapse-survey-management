using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.DAL.Interfaces;
using SynapseEnqueteManagement.BL.Interfaces;

namespace SynapseEnqueteManagement.BL.Implementation;

public class ParticipantManager(
    IParticipantRepository participantRepository,
    IUnitOfWork uow) : IParticipantManager
{
    public async Task<Participant> AddParticipantAsync(Project project, CancellationToken ct = default)
    {
        var participant = new Participant
        {
            Project = project,
            CreatedAt = DateTime.UtcNow
        };
        var created = await participantRepository.CreateAsync(participant);
        await uow.SaveChangesAsync(ct);
        return created;
    }

    public Task<Participant> GetParticipantByIdReadOnlyAsync(int participantId)
        => participantRepository.ReadByIdReadOnlyAsync(participantId);

    public Task<Participant> GetParticipantBySessionTokenForUpdateAsync(Guid sessionToken)
        => participantRepository.ReadBySessionTokenWithProjectForUpdateAsync(sessionToken);

    public async Task<bool> HasParticipantSubmittedAsync(Guid sessionToken)
    {
        var participant = await participantRepository.ReadBySessionTokenReadOnlyAsync(sessionToken);
        return participant?.HasSubmitted ?? false;
    }

    public async Task MarkAsSubmittedAsync(Guid sessionToken, CancellationToken ct = default)
    {
        var participant = await participantRepository.ReadBySessionTokenForUpdateAsync(sessionToken);
        if (participant is null)
            throw new KeyNotFoundException("Participant not found.");
        participant.HasSubmitted = true;
        participant.CompletedAt = DateTime.UtcNow;
        participantRepository.Update(participant);
        await uow.SaveChangesAsync(ct);
    }

    public Task<int> CountCompletionsByProjectAsync(int projectId)
        => participantRepository.CountCompletionsByProjectAsync(projectId);

    public async Task SaveContactInfoAsync(int participantId, string email, CancellationToken ct = default)
    {
        var participant = await participantRepository.ReadByIdForUpdateAsync(participantId);
        if (participant is null)
            throw new KeyNotFoundException($"Participant with id {participantId} not found.");
        participant.Email = email;
        participantRepository.Update(participant);
        await uow.SaveChangesAsync(ct);
    }
}
