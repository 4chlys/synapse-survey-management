using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;
using SynapseEnqueteManagement.DAL;

namespace SynapseEnqueteManagement.BL;

public class IdeaManager
{
    private readonly IRepository _repository;
    
    public IEnumerable<Idea> GetIdeasByCategory(int categoryId)
    {
        return _repository.Read<Idea>(
            filters: [i => i.Category.CategoryId == categoryId],
            includes: [i => i.Comments]
        );
    }

    public Idea AddIdea(int categoryId, string description)
    {
        var category = _repository.ReadById<Category>(categoryId);
        if (category == null) throw new Exception("Category not found");

        var idea = new Idea
        {
            Description = description,
            Category = category,
            CommentCount = 0
        };

        return _repository.Create(idea);
    }
    
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
            includes: [i => i.Comments, i => i.EmojiReactions]
        );
    }

}