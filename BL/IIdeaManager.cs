using SynapseEnqueteManagement.BL.Domain.Project.Entities;

namespace SynapseEnqueteManagement.BL;

public interface IIdeaManager
{
    Task<IEnumerable<Idea>> GetIdeasByCategoryAsync(int categoryId);
    Task<Idea> AddIdeaAsync(int categoryId, string description);
    Task<TextComment> AddCommentAsync(int ideaId, string text);
    Task<Idea> GetIdeaByIdAsync(int ideaId);
    Task<IEnumerable<Emoji>> GetEmojisAsync();
    Task<IconComment> AddIconCommentAsync(int ideaId, int emojiId);
    Task<CommentEmoji> AddCommentEmojiAsync(int commentId, int emojiId);
}