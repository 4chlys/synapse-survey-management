using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;
using SynapseEnqueteManagement.BL.Domain.Project.Stats;

namespace SynapseEnqueteManagement.BL.Interfaces;

public interface IIdeaManager
{
    Task<IdeaIndexResult> GetIdeasIndexAsync(int categoryId, int projectId, int? participantId, int pageSize, int page, string sortKey, string participantLanguageCode = null, bool showAllLanguages = false, CancellationToken ct = default);
    Task<IEnumerable<Idea>> GetParticipantIdeasByCategoryWithCommentsReadOnlyAsync(int categoryId, int participantId);
    Task<Idea> GetIdeaByIdWithCommentsReadOnlyAsync(int ideaId);
    Task<int?> GetIdeaAuthorParticipantIdReadOnlyAsync(int ideaId);
    Task<AddIdeaResult> AddIdeaAsync(int categoryId, int projectId, string description, ModerationStatus status, Participant participant, string imageUrl = null, string submissionLanguageCode = null, CancellationToken ct = default);
    /// <summary>
    /// Clears the idea's ImageUrl in the DB and returns the old URL so the caller can delete from storage.
    /// Returns null if the idea had no image.
    /// </summary>
    Task<string> RemoveIdeaImageAsync(int ideaId, int projectId, CancellationToken ct = default);
    Task<int> CountVisibleByCategoryAsync(int categoryId, string filterLanguageCode = null);
    Task<Dictionary<int, int>> CountVisibleByCategoriesAsync(IEnumerable<int> categoryIds, string filterLanguageCode = null);

    Task<List<IdeaMetric>> GetTopIdeasAsync(int projectId, int count);
    Task<List<Idea>> GetLatestApprovedWithCommentsAsync(int projectId, int count);
    Task<Dictionary<ModerationStatus, int>> CountByStatusAsync(int projectId, string search = null, int? categoryId = null);

    Task<(List<Idea> Items, int Total)> GetIdeasPageAsync(
        int projectId, string search, int? categoryId, ModerationStatus? status, int page, int pageSize);

    Task SetModerationStatusAsync(int ideaId, ModerationStatus status, int? projectId = null);
}
