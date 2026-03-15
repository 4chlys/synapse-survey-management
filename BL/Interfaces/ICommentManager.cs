using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;
using SynapseEnqueteManagement.BL.Implementation;

namespace SynapseEnqueteManagement.BL.Interfaces;

public interface ICommentManager
{
    Task<PagedResult<TextComment>> GetCommentsWithOwnFirstAsync(int ideaId, int page, int pageSize, string sortKey, int? participantId);
    Task<TextComment> AddTextCommentAsync(int ideaId, int projectId, string text, ModerationStatus status, Participant participant);
    Task<PagedResult<TextComment>> GetOkCommentsByIdeaAsync(int ideaId, int page, int pageSize, string sortKey, int? excludeParticipantId = null);
    Task<IEnumerable<TextComment>> GetOwnCommentsByIdeaAsync(int ideaId, int participantId);
    Task<IEnumerable<Emoji>> GetEmojisAsync();
    Task<IconComment> AddIconCommentAsync(int ideaId, int projectId, int emojiId, Participant participant);
    Task<bool> RemoveIconCommentAsync(int ideaId, int projectId, int emojiId, Participant participant);
    Task<IconComment> AddCommentReactionAsync(int commentId, int projectId, int emojiId, Participant participant);
    Task<bool> RemoveCommentReactionAsync(int commentId, int projectId, int emojiId, Participant participant);
}