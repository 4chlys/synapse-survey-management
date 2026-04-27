using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;
using SynapseEnqueteManagement.BL.Interfaces;
using SynapseEnqueteManagement.DAL.Interfaces;

namespace SynapseEnqueteManagement.BL.Implementation;

public class CategoryManager(
    ICategoryRepository categoryRepository,
    IUnitOfWork uow) : ICategoryManager
{
    public Task<IEnumerable<Category>> GetCategoriesForProjectReadOnlyAsync(int projectId)
        => categoryRepository.ReadByProjectIdReadOnlyAsync(projectId);

    public Task<IEnumerable<Category>> GetCategoriesWithIdeasForProjectReadOnlyAsync(int projectId)
        => categoryRepository.ReadAllWithIdeasByProjectReadOnlyAsync(projectId);

    public Task<Category> GetCategoryByIdReadOnlyAsync(int id)
        => categoryRepository.ReadByIdReadOnlyAsync(id);

    public Task<Category> GetCategoryByIdWithProjectReadOnlyAsync(int categoryId)
        => categoryRepository.ReadByIdWithProjectReadOnlyAsync(categoryId);

    public async Task<Category> CreateCategoryAsync(Project project, string name, string description, CancellationToken ct = default)
    {
        var category = new Category
        {
            Name = name,
            Description = description,
            Project = project
        };
        EntityValidator.Validate(category);
        var created = await categoryRepository.CreateAsync(category);
        await uow.SaveChangesAsync(ct);
        return created;
    }

    public async Task<bool> UpdateCategoryAsync(int categoryId, string name, string description, CancellationToken ct = default)
    {
        var category = await categoryRepository.ReadByIdForUpdateAsync(categoryId);
        if (category is null) return false;

        EntityValidator.ValidateProperty(category, name, nameof(Category.Name));
        EntityValidator.ValidateProperty(category, description, nameof(Category.Description));
        category.Name = name;
        category.Description = description;
        categoryRepository.Update(category);
        await uow.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> DeleteCategoryAsync(int categoryId, CancellationToken ct = default)
    {
        var result = await categoryRepository.DeleteByIdAsync(categoryId);
        if (result) await uow.SaveChangesAsync(ct);
        return result;
    }

    public async Task<List<IdeaFeedItem>> GetIdeaFeedAsync(int projectId, int count = 10)
    {
        var ideas = await categoryRepository.ReadLatestIdeasAsync(projectId, count);
        return ideas.Select(i => new IdeaFeedItem(
            IdeaId: i.IdeaId,
            Description: i.Description.Length > 80 ? i.Description[..80] + "…" : i.Description,
            CategoryName: i.Category.Name,
            ReactionCount: i.Comments.OfType<IconComment>().Count(),
            CreatedAt: i.CreatedAt
        )).ToList();
    }

    public async Task<ModerationQueueResult> GetModerationQueueAsync(int projectId)
    {
        var counts = await categoryRepository.ReadModerationQueueAsync(projectId);
        return new ModerationQueueResult(
            Approved: counts.GetValueOrDefault(ModerationStatus.Approved, 0),
            Declined: counts.GetValueOrDefault(ModerationStatus.Declined, 0),
            Flagged: counts.GetValueOrDefault(ModerationStatus.Flagged, 0),
            Unflagged: counts.GetValueOrDefault(ModerationStatus.Unflagged, 0)
        );
    }
}
