using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;

namespace SynapseEnqueteManagement.BL.Interfaces;

public interface ICommentManager
{
    Task<PagedResult<TextComment>> GetCommentsWithOwnFirstReadOnlyAsync(int projectId, int ideaId, int page, string sortKey, int pageSize, int? participantId);
    Task<TextComment> AddTextCommentAsync(int ideaId, int projectId, string text, ModerationStatus status, Participant participant, CancellationToken ct = default);
    Task<IconComment> AddIconCommentAsync(int ideaId, int projectId, int emojiId, Participant participant, CancellationToken ct = default);
    Task<bool> RemoveIconCommentAsync(int ideaId, int projectId, int emojiId, Participant participant, CancellationToken ct = default);
    Task<IconComment> AddCommentReactionAsync(int commentId, int projectId, int emojiId, Participant participant, CancellationToken ct = default);
    Task<bool> RemoveCommentReactionAsync(int commentId, int projectId, int emojiId, Participant participant, CancellationToken ct = default);
}