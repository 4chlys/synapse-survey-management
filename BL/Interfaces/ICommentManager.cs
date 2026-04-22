using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;

namespace SynapseEnqueteManagement.BL.Interfaces;

public interface ICommentManager
{
    Task<PagedResult<TextComment>> GetCommentsWithOwnFirstAsync(int ideaId, int page, int pageSize, string sortKey, int? participantId);
    Task<TextComment> AddTextCommentAsync(int ideaId, int projectId, string text, ModerationStatus status, Participant participant, CancellationToken ct = default);
    Task<PagedResult<TextComment>> GetOkCommentsByIdeaAsync(int ideaId, int page, int pageSize, string sortKey, int? excludeParticipantId = null);
    Task<IEnumerable<TextComment>> GetOwnCommentsByIdeaAsync(int ideaId, int participantId);
    Task<IEnumerable<Emoji>> GetEmojisAsync(int projectId);
    Task<IconComment> AddIconCommentAsync(int ideaId, int projectId, int emojiId, Participant participant, CancellationToken ct = default);
    Task<bool> RemoveIconCommentAsync(int ideaId, int projectId, int emojiId, Participant participant, CancellationToken ct = default);
    Task<IconComment> AddCommentReactionAsync(int commentId, int projectId, int emojiId, Participant participant, CancellationToken ct = default);
    Task<bool> RemoveCommentReactionAsync(int commentId, int projectId, int emojiId, Participant participant, CancellationToken ct = default);
}