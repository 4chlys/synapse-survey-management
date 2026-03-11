using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;
using SynapseEnqueteManagement.DAL;

namespace SynapseEnqueteManagement.BL;

public class IdeaManager : IIdeaManager
{
    private readonly IRepository _repository;
    
    public async Task<TextComment> AddCommentAsync(int ideaId, string text)
    {
        var idea = await _repository.ReadByIdAsync<Idea>(ideaId);
        if (idea == null) throw new Exception("Idea not found");

        var comment = new TextComment
        {
            IdeaId = ideaId,
            Text = text
        };

        return await _repository.CreateAsync(comment);
    }
    
    public async Task<Idea> GetIdeaByIdAsync(int ideaId)
    {
        return await _repository.ReadSingleAsync<Idea>(
            filters: [i => i.IdeaId == ideaId],
            includes: [i => i.Comments],
            includeStrings: ["Comments.Emoji", "Comments.EmojiReactions.Emoji"]
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
        if (idea == null) throw new Exception("Idea not found");

        var emoji = await _repository.ReadByIdAsync<Emoji>(emojiId);
        if (emoji == null) throw new Exception("Emoji not found");

        var iconComment = new IconComment
        {
            IdeaId = ideaId,
            EmojiId = emojiId
        };

        return await _repository.CreateAsync(iconComment);
    }
    
    public async Task<CommentEmoji> AddCommentEmojiAsync(int commentId, int emojiId)
    {
        var comment = await _repository.ReadByIdAsync<Comment>(commentId);
        if (comment == null) throw new Exception("Comment not found");

        var emoji = await _repository.ReadByIdAsync<Emoji>(emojiId);
        if (emoji == null) throw new Exception("Emoji not found");

        var commentEmoji = new CommentEmoji
        {
            CommentId = commentId,
            EmojiId = emojiId
        };

        return await _repository.CreateAsync(commentEmoji);
    }
    
    public IdeaManager(IRepository repository)
    {
        _repository = repository;
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
        if (category == null) throw new Exception("Category not found");

        var idea = new Idea
        {
            Description = description,
            Category = category,
            CommentCount = 0
        };

        return await _repository.CreateAsync(idea);
    }
}