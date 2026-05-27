using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Domain.Stats;

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

    Task<IReadOnlyList<ProjectParticipantStats>> ReadStatsByProjectsReadOnlyAsync(IEnumerable<int> projectIds);
}
