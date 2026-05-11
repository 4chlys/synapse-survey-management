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
            Questionnaire = project.Questionnaire,
            CreatedAt = DateTime.UtcNow
        };
        EntityValidator.Validate(participant);
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

    public Task<List<string>> GetParticipantEmailsByProjectAsync(int projectId)
        => participantRepository.ReadEmailsByProjectIdReadOnlyAsync(projectId);

    public async Task<TimelineResult> GetTimelineAsync(int projectId, int days = 30)
    {
        var events = await participantRepository.ReadTimelineAsync(projectId, days);
        var cutoff = DateTime.UtcNow.Date.AddDays(-days + 1);
        var allDates = Enumerable.Range(0, days)
            .Select(i => cutoff.AddDays(i))
            .ToList();

        var startsByDate = events.Where(e => !e.IsCompletion)
            .GroupBy(e => e.Date)
            .ToDictionary(g => g.Key, g => g.Count());

        var completionsByDate = events.Where(e => e.IsCompletion)
            .GroupBy(e => e.Date)
            .ToDictionary(g => g.Key, g => g.Count());

        return new TimelineResult(
            Dates: allDates.Select(d => d.ToString("yyyy-MM-dd")).ToList(),
            Starts: allDates.Select(d => startsByDate.GetValueOrDefault(d, 0)).ToList(),
            Completions: allDates.Select(d => completionsByDate.GetValueOrDefault(d, 0)).ToList()
        );
    }

    public async Task SaveContactInfoAsync(int participantId, string email, CancellationToken ct = default)
    {
        var participant = await participantRepository.ReadByIdForUpdateAsync(participantId);
        if (participant is null)
            throw new KeyNotFoundException($"Participant with id {participantId} not found.");
        EntityValidator.ValidateProperty(participant, email, nameof(Participant.Email));
        participant.Email = email;
        participantRepository.Update(participant);
        await uow.SaveChangesAsync(ct);
    }
}
