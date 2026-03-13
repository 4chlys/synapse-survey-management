using Microsoft.Extensions.Logging;
using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.DAL;

namespace SynapseEnqueteManagement.BL;

public class IdeaManager(IRepository repository, ILogger<IdeaManager> logger) : IIdeaManager
{
    public async Task<IEnumerable<Idea>> GetIdeasByCategoryAsync(int categoryId)
    {
        return await repository.ReadAsync<Idea>(
            filters: [i => i.Category.CategoryId == categoryId],
            includes: [i => i.Comments],
            includeStrings: ["Comments.Emoji"]
        );
    }

    public async Task<Idea> AddIdeaAsync(int categoryId, string description)
    {
        var category = await repository.ReadByIdAsync<Category>(categoryId);
        if (category == null)
        {
            logger.LogWarning("Category {CategoryId} not found", categoryId);
            throw new KeyNotFoundException($"Category {categoryId} not found");
        }

        var idea = new Idea
        {
            Description = description,
            Category = category
        };

        return await repository.CreateAsync(idea);
    }

    public async Task<TextComment> AddCommentAsync(int ideaId, string text)
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
            Text = text
        };

        return await repository.CreateAsync(comment);
    }

    public async Task<Idea> GetIdeaByIdAsync(int ideaId)
    {
        return await repository.ReadSingleAsync<Idea>(
            filters: [i => i.IdeaId == ideaId],
            includes: [i => i.Comments],
            includeStrings: [
                "Comments.Emoji",
                "Comments.Reactions",
                "Comments.Reactions.Emoji"
            ]
        );
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