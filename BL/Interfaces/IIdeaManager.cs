using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;

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
    Task<Dictionary<int, int>> CountByCategoriesAsync(IEnumerable<int> categoryIds);
    Task<int> CountByCategoryAsync(int categoryId);
}
