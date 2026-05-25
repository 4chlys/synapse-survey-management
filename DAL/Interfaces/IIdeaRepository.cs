using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;

namespace SynapseEnqueteManagement.DAL.Interfaces;

public interface IIdeaRepository : IRepository<Idea>
{
    Task<Idea> ReadByIdWithCommentsAndParticipantReadOnlyAsync(int ideaId);

    Task<Idea> ReadByIdWithParticipantReadOnlyAsync(int ideaId);

    Task<Idea> ReadByIdWithProjectForUpdateAsync(int ideaId);

    Task<Idea> ReadByIdWithProjectAndEmojisForUpdateAsync(int ideaId);

    Task<(bool AllowCrossLanguageIdeas, string DefaultLanguageCode)> ReadProjectFlagsForCategoryReadOnlyAsync(int categoryId);

    Task<int> CountVisibleByCategoryAsync(int categoryId, string filterLanguageCode = null);

    Task<Dictionary<int, int>> CountVisibleByCategoriesAsync(IEnumerable<int> categoryIds, string filterLanguageCode = null);

    Task<IEnumerable<Idea>> ReadVisibleByCategoryWithCommentsReadOnlyAsync(
        int categoryId,
        Func<IQueryable<Idea>, IOrderedQueryable<Idea>> orderBy,
        bool randomize,
        int skip,
        int take,
        string filterLanguageCode = null);

    Task<IEnumerable<Idea>> ReadByCategoryAndParticipantWithCommentsReadOnlyAsync(int categoryId, int participantId);

    Task<bool> AnyExistByParticipantAsync(int participantId);

    // Description of the participant's most recent idea in this category, used as
    // the anchor for similar/different/diverse embedding sorts. Null = no idea yet.
    Task<string> ReadMostRecentDescriptionByCategoryAndParticipantAsync(int categoryId, int participantId);

    Task<List<Idea>> ReadForSummaryAsync(
        int projectId,
        int? categoryId,
        ModerationStatus? status,
        string languageCode,
        CancellationToken ct = default);
}
