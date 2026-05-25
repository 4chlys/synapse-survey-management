using SynapseEnqueteManagement.BL.Domain.Project.Entities;

namespace SynapseEnqueteManagement.DAL.Interfaces;

public interface ICommentRepository : IRepository<Comment>
{
    Task<int> CountOkTextCommentsByIdeaAsync(int ideaId, int? excludeParticipantId);

    Task<IEnumerable<TextComment>> ReadOkTextCommentsByIdeaWithReactionsReadOnlyAsync(
        int ideaId,
        int? excludeParticipantId,
        Func<IQueryable<TextComment>, IOrderedQueryable<TextComment>> orderBy,
        bool randomize,
        int skip,
        int take);

    Task<IEnumerable<TextComment>> ReadOwnTextCommentsByIdeaWithReactionsReadOnlyAsync(int ideaId, int participantId);

    Task<TextComment> ReadTextCommentByIdWithProjectAndEmojisForUpdateAsync(int commentId);

    Task<IconComment> CreateIconCommentOnIdeaIfNotDuplicateAsync(IconComment iconComment);

    Task<IconComment> CreateIconCommentOnTextCommentIfNotDuplicateAsync(IconComment iconComment);

    Task<bool> DeleteIconCommentOnIdeaAsync(int ideaId, int projectId, int emojiId, int participantId);

    Task<bool> DeleteIconCommentOnTextCommentAsync(int commentId, int projectId, int emojiId, int participantId);
}
