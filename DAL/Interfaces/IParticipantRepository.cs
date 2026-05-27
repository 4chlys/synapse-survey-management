using SynapseEnqueteManagement.BL.Domain.Project.Entities;

namespace SynapseEnqueteManagement.DAL.Interfaces;

public interface IParticipantRepository : IRepository<Participant>
{
    Task<Participant> ReadByIdReadOnlyAsync(int participantId);

    Task<Participant> ReadBySessionTokenWithProjectForUpdateAsync(Guid sessionToken);

    Task<Participant> ReadBySessionTokenReadOnlyAsync(Guid sessionToken);

    Task<Participant> ReadBySessionTokenForUpdateAsync(Guid sessionToken);

    Task<int> CountCompletionsByProjectAsync(int projectId);
    Task<List<(DateTime Date, bool IsCompletion)>> ReadTimelineAsync(int projectId, int days);

    Task<List<string>> ReadEmailsByProjectIdReadOnlyAsync(int projectId);

    // Same data as ReadEmailsByProjectIdReadOnlyAsync but grouped with the
    // participant's chosen language — needed by admin broadcasts to send one
    // translation per language group.
    Task<List<ParticipantEmailWithLanguage>> ReadEmailsWithLanguageByProjectIdReadOnlyAsync(int projectId);

    Task<IReadOnlyList<ProjectParticipantStats>> ReadStatsByProjectsReadOnlyAsync(IEnumerable<int> projectIds);
}
