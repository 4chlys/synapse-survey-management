using Microsoft.Extensions.Logging;
using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;
using SynapseEnqueteManagement.DAL;

namespace SynapseEnqueteManagement.BL;

public class IdeaManager(IRepository repository, ILogger<IdeaManager> logger) : IIdeaManager
{
    public async Task<IEnumerable<Idea>> GetIdeasByCategoryAsync(int categoryId)
    {
        return await repository.ReadAsync<Idea>(
            filters: [i => i.Category.CategoryId == categoryId],
            includes: [i => i.Comments],
            includeStrings: ["Comments.Emoji", "Comments.Participant"]
        );
    }

    public async Task<Idea> AddIdeaAsync(int categoryId, string description, ModerationStatus status,
        Participant participant)
    {
        var category = await repository.ReadByIdAsync<Category>(categoryId);
        if (category == null)
        {
            logger.LogWarning("Category {CategoryId} not found", categoryId);
            throw new KeyNotFoundException($"Category {categoryId} not found");
        }

        var idea = new Idea
        {
            Status = status,
            Description = description,
            Category = category,
            Participant = participant
        };

        return await repository.CreateAsync(idea);
    }

    public async Task<Idea> GetIdeaByIdAsync(int ideaId)
    {
        return await repository.ReadByIdAsync<Idea>(
            ideaId,
            includes: [i => i.Comments],
            includeStrings:
            [
                "Comments.Emoji",
                "Comments.Participant",
                "Comments.EmojiReactions",
                "Comments.EmojiReactions.Emoji",
                "Comments.EmojiReactions.Participant"
            ]
        );
    }
}