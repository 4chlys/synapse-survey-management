using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;

namespace SynapseEnqueteManagement.BL.Interfaces;

public interface IIdeaManager
{
    Task<PagedResult<Idea>> GetOkIdeasByCategoryAsync(int categoryId, int page, int pageSize, string sortKey, int? excludeParticipantId = null);

    // Embedding-based sort variant: the sortKey selects a strategy in
    // IEmbeddingSortStrategyFactory (similar / different / diverse). For similar /
    // different, queryText is required and is embedded on the fly; for diverse
    // it may be null (falls back to corpus centroid).
    Task<PagedResult<Idea>> GetOkIdeasByCategoryEmbeddingSortedAsync(
        int categoryId, int page, int pageSize, string sortKey,
        string queryText, int? excludeParticipantId = null,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<Idea>> GetOwnIdeasByCategoryAsync(int categoryId, int participantId);
    Task<Idea> AddIdeaAsync(int categoryId, int projectId, string description, ModerationStatus status, Participant participant);
    Task<Idea> GetIdeaByIdAsync(int ideaId);
    Task<int> GetIdeaCountByParticipantAsync(int participantId);
}