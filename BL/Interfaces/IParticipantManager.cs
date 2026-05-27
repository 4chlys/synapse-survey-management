using SynapseEnqueteManagement.BL.Domain.I18n.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.DAL;

namespace SynapseEnqueteManagement.BL.Interfaces;

public interface IParticipantManager
{
    Task<Participant> AddParticipantAsync(Project project, string languageCode, CancellationToken ct = default);
    Task<Language> SetLanguageAsync(int participantId, string languageCode, CancellationToken ct = default);
    Task<Participant> GetParticipantByIdReadOnlyAsync(int participantId);
    Task<Participant> GetParticipantBySessionTokenForUpdateAsync(Guid sessionToken);
    Task<bool> HasParticipantSubmittedAsync(Guid sessionToken);
    Task MarkAsSubmittedAsync(Guid sessionToken, CancellationToken ct = default);
    Task SaveContactInfoAsync(int participantId, string email, CancellationToken ct = default);
    Task<int> CountCompletionsByProjectAsync(int projectId);
    Task<TimelineResult> GetTimelineAsync(int projectId, int days = 30);

    Task<List<string>> GetParticipantEmailsByProjectAsync(int projectId);
    Task<List<ParticipantEmailWithLanguage>> GetParticipantEmailsWithLanguageByProjectAsync(int projectId);
    Task<IReadOnlyList<ProjectParticipantStats>> GetStatsByProjectsAsync(IEnumerable<int> projectIds);
}
