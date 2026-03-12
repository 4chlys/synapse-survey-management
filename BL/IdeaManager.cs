using Microsoft.Extensions.Logging;
using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.DAL;

namespace SynapseEnqueteManagement.BL;

public class IdeaManager : IIdeaManager
{
    private readonly IRepository _repository;
    private readonly ILogger<IdeaManager> _logger;

    public IdeaManager(IRepository repository, ILogger<IdeaManager> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<IEnumerable<Idea>> GetIdeasByCategoryAsync(int categoryId)
    {
        return await _repository.ReadAsync<Idea>(
            filters: [i => i.Category.CategoryId == categoryId],
            includes: [i => i.Comments],
            includeStrings: ["Comments.Emoji"]
        );
    }

    public async Task<Idea> AddIdeaAsync(int categoryId, string description)
    {
        var category = await _repository.ReadByIdAsync<Category>(categoryId);
        if (category == null)
        {
            _logger.LogWarning("Category {CategoryId} not found", categoryId);
            throw new KeyNotFoundException($"Category {categoryId} not found");
        }

        var idea = new Idea
        {
            Description = description,
            Category = category
        };

        return await _repository.CreateAsync(idea);
    }

    public async Task<TextComment> AddCommentAsync(int ideaId, string text)
    {
        var idea = await _repository.ReadByIdAsync<Idea>(ideaId);
        if (idea == null)
        {
            _logger.LogWarning("Idea {IdeaId} not found", ideaId);
            throw new KeyNotFoundException($"Idea {ideaId} not found");
        }

        var comment = new TextComment
        {
            Idea = idea,
            Text = text
        };

        return await _repository.CreateAsync(comment);
    }

    public async Task<Idea> GetIdeaByIdAsync(int ideaId)
    {
        return await _repository.ReadByIdAsync<Idea>(
            ideaId,
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
        return await _repository.ReadAsync<Emoji>(
            filters: [e => e.IsActive]
        );
    }

    public async Task<IconComment> AddIconCommentAsync(int ideaId, int emojiId)
    {
        var idea = await _repository.ReadByIdAsync<Idea>(ideaId);
        if (idea == null)
        {
            _logger.LogWarning("Idea {IdeaId} not found", ideaId);
            throw new KeyNotFoundException($"Idea {ideaId} not found");
        }

        var emoji = await _repository.ReadByIdAsync<Emoji>(emojiId);
        if (emoji == null)
        {
            _logger.LogWarning("Emoji {EmojiId} not found", emojiId);
            throw new KeyNotFoundException($"Emoji {emojiId} not found");
        }

        var iconComment = new IconComment
        {
            Idea = idea,
            EmojiId = emojiId
        };

        return await _repository.CreateAsync(iconComment);
    }

    public async Task<IconComment> AddCommentReactionAsync(int commentId, int emojiId)
    {
        var comment = await _repository.ReadByIdAsync<TextComment>(commentId, includes: [c => c.Idea]);
        if (comment == null)
        {
            _logger.LogWarning("TextComment {CommentId} not found", commentId);
            throw new KeyNotFoundException($"TextComment {commentId} not found");
        }

        var emoji = await _repository.ReadByIdAsync<Emoji>(emojiId);
        if (emoji == null)
        {
            _logger.LogWarning("Emoji {EmojiId} not found", emojiId);
            throw new KeyNotFoundException($"Emoji {emojiId} not found");
        }

        var reaction = new IconComment
        {
            Idea = comment.Idea,
            EmojiId = emojiId,
            TextComment = comment
        };

        return await _repository.CreateAsync(reaction);
    }
}