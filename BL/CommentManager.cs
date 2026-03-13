using Microsoft.Extensions.Logging;
using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;
using SynapseEnqueteManagement.DAL;

namespace SynapseEnqueteManagement.BL;

public class CommentManager(IRepository repository, ILogger<CommentManager> logger) : ICommentManager
{
    public async Task<TextComment> AddTextCommentAsync(int ideaId, string text, ModerationStatus status)
    {
        var idea = await repository.ReadByIdAsync<Idea>(ideaId);
        if (idea == null)
        {
            logger.LogWarning("Idea {IdeaId} not found", ideaId);
            throw new KeyNotFoundException($"Idea {ideaId} not found");
        }

        var comment = new TextComment
        {
            Idea = idea,
            ModerationStatus = status,
            Text = text
        };

        return await repository.CreateAsync(comment);
    }

    public async Task<IEnumerable<Emoji>> GetEmojisAsync()
    {
        return await repository.ReadAsync<Emoji>(
            filters: [e => e.IsActive]
        );
    }

    public async Task<IconComment> AddIconCommentAsync(int ideaId, int emojiId)
    {
        var idea = await repository.ReadByIdAsync<Idea>(ideaId);
        if (idea == null)
        {
            logger.LogWarning("Idea {IdeaId} not found", ideaId);
            throw new KeyNotFoundException($"Idea {ideaId} not found");
        }

        var emoji = await repository.ReadByIdAsync<Emoji>(emojiId);
        if (emoji == null)
        {
            logger.LogWarning("Emoji {EmojiId} not found", emojiId);
            throw new KeyNotFoundException($"Emoji {emojiId} not found");
        }

        var iconComment = new IconComment
        {
            Idea = idea,
            EmojiId = emojiId
        };

        return await repository.CreateAsync(iconComment);
    }

    public async Task<IconComment> AddCommentReactionAsync(int commentId, int emojiId)
    {
        var comment = await repository.ReadByIdAsync<TextComment>(commentId);
        if (comment == null)
        {
            logger.LogWarning("TextComment {CommentId} not found", commentId);
            throw new KeyNotFoundException($"TextComment {commentId} not found");
        }

        var emoji = await repository.ReadByIdAsync<Emoji>(emojiId);
        if (emoji == null)
        {
            logger.LogWarning("Emoji {EmojiId} not found", emojiId);
            throw new KeyNotFoundException($"Emoji {emojiId} not found");
        }

        var reaction = new IconComment
        {
            Idea = comment.Idea,
            EmojiId = emojiId,
            TextComment = comment
        };

        return await repository.CreateAsync(reaction);
    }
}