using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;
using SynapseEnqueteManagement.BL.Domain.Project.Stats;
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

    public async Task<DashboardMetrics> GetDashboardMetricsAsync(int projectId, int topCount = 5)
    {
        var categoriesTask = categoryRepository.ReadTopCategoriesAsync(projectId, topCount);
        var ideasTask = categoryRepository.ReadTopIdeasAsync(projectId, topCount);
        await Task.WhenAll(categoriesTask, ideasTask);

        var ideas = (await ideasTask).Select(i => i with
        {
            Description = i.Description.Length > 100 ? i.Description[..100] + "…" : i.Description
        }).ToList();

        return new DashboardMetrics(await categoriesTask, ideas);
    }

    public async Task<List<IdeaFeedItem>> GetIdeaFeedAsync(int projectId, int count = 10)
    {
        var ideas = await categoryRepository.ReadLatestIdeasAsync(projectId, count);
        return ideas.Select(i => new IdeaFeedItem(
            IdeaId: i.IdeaId,
            Description: i.Description.Length > 80 ? i.Description[..80] + "…" : i.Description,
            CategoryName: i.Category.Name,
            CategoryId: i.Category.CategoryId,
            ReactionCount: i.Comments.OfType<IconComment>().Count(ic => ic.TextComment == null),
            CreatedAt: i.CreatedAt,
            Status: i.Status,
            ParticipantEmail: i.Participant?.Email
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

    public async Task<(List<IdeaFeedItem> Items, int Total, int ApprovedCount, int PendingCount, int FlaggedCount)>
        GetIdeasPagedAsync(int projectId, string search, int? categoryId, ModerationStatus? status, int page, int pageSize = 20)
    {
        var (ideas, total) = await categoryRepository.ReadIdeasPagedAsync(
            projectId, search, categoryId, status, page, pageSize);

        // Global counts (unfiltered) for summary bar — single GROUP BY query
        var queue    = await categoryRepository.ReadModerationQueueAsync(projectId);
        var approved = queue.GetValueOrDefault(ModerationStatus.Approved, 0);
        var pending  = queue.GetValueOrDefault(ModerationStatus.Unflagged, 0);
        var flagged  = queue.GetValueOrDefault(ModerationStatus.Flagged, 0);

        var items = ideas.Select(i => new IdeaFeedItem(
            IdeaId:        i.IdeaId,
            Description:   i.Description,
            CategoryName:  i.Category.Name,
            CategoryId:    i.Category.CategoryId,
            ReactionCount: i.Comments.OfType<IconComment>().Count(),
            CreatedAt:     i.CreatedAt,
            Status:        i.Status,
            ParticipantEmail: i.Participant?.Email,
            ImageUrl:      i.ImageUrl
        )).ToList();

        return (items, total, approved, pending, flagged);
    }

    public async Task SetIdeaModerationStatusAsync(int ideaId, ModerationStatus status, int? projectId = null)
    {
        var idea = await categoryRepository.ReadIdeaByIdForUpdateAsync(ideaId, projectId)
            ?? throw new KeyNotFoundException($"Idea {ideaId} not found.");
        idea.Status = status;
        await uow.SaveChangesAsync();
    }
}
