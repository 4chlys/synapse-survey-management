using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;

namespace SynapseEnqueteManagement.BL;

public interface ICommentManager
{
    Task<TextComment> AddTextCommentAsync(int ideaId, string text, ModerationStatus status);
    Task<IEnumerable<Emoji>> GetEmojisAsync();
    Task<IconComment> AddIconCommentAsync(int ideaId, int emojiId);
    Task<IconComment> AddCommentReactionAsync(int commentId, int emojiId);
}