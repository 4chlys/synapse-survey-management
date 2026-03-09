using SynapseEnqueteManagement.BL.Domain.Project.Entities;
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
            EmojiCount = 0,
            CommentCount = 0
        };

        return _repository.Create(idea);
    }

}