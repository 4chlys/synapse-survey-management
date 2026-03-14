using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;

namespace SynapseEnqueteManagement.BL;

public interface ICommentManager
{
    Task<TextComment> AddTextCommentAsync(int ideaId, int projectId, string text, ModerationStatus status, Participant participant);
    Task<PagedResult<TextComment>> GetOkCommentsByIdeaAsync(int ideaId, int page, int pageSize, string sortKey);
    Task<IEnumerable<Emoji>> GetEmojisAsync();
    Task<IconComment> AddIconCommentAsync(int ideaId, int emojiId, Participant participant);
    Task<bool> RemoveIconCommentAsync(int ideaId, int emojiId, Participant participant);
    Task<IconComment> AddCommentReactionAsync(int commentId, int emojiId, Participant participant);
    Task<bool> RemoveCommentReactionAsync(int commentId, int emojiId, Participant participant);
}