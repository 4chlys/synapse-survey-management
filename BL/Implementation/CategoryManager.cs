using System.ComponentModel.DataAnnotations;
using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;
using SynapseEnqueteManagement.BL.Domain.Project.Stats;
using SynapseEnqueteManagement.BL.Exceptions;
using SynapseEnqueteManagement.BL.Interfaces;
using SynapseEnqueteManagement.DAL.Interfaces;

namespace SynapseEnqueteManagement.BL.Implementation;

public class CategoryManager(
    ICategoryRepository categoryRepository,
    IIdeaRepository ideaRepository,
    ILanguageRepository languageRepository,
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

    public async Task<Category> CreateCategoryAsync(Project project, IEnumerable<CategoryTranslationInput> translations, CancellationToken ct = default)
    {
        var translationList = translations?.ToList() ?? new List<CategoryTranslationInput>();
        var languages = await ValidateTranslationInputsAsync(translationList, project);

        var category = new Category { Project = project };

        foreach (var input in translationList)
        {
            EntityValidator.ValidateProperty(new CategoryTranslation(), input.Name, nameof(CategoryTranslation.Name));
            EntityValidator.ValidateProperty(new CategoryTranslation(), input.Description, nameof(CategoryTranslation.Description));
            category.Translations.Add(new CategoryTranslation
            {
                Language = languages[input.LanguageCode],
                Name = input.Name,
                Description = input.Description,
            });
        }

        var created = await categoryRepository.CreateAsync(category);
        await uow.SaveChangesAsync(ct);
        return created;
    }

    public async Task<bool> UpdateCategoryAsync(int categoryId, IEnumerable<CategoryTranslationInput> translations, CancellationToken ct = default)
    {
        var category = await categoryRepository.ReadByIdWithProjectForUpdateAsync(categoryId);
        if (category is null) return false;

        var translationList = translations?.ToList() ?? new List<CategoryTranslationInput>();
        var languages = await ValidateTranslationInputsAsync(translationList, category.Project);

        void Validate(CategoryTranslationInput input)
        {
            EntityValidator.ValidateProperty(new CategoryTranslation(), input.Name, nameof(CategoryTranslation.Name));
            EntityValidator.ValidateProperty(new CategoryTranslation(), input.Description, nameof(CategoryTranslation.Description));
        }

        TranslationUpserter.Upsert(
            category.Translations,
            translationList,
            inputLanguageCode: i => i.LanguageCode,
            existingLanguageCode: t => t.Language.Code,
            create: input =>
            {
                Validate(input);
                return new CategoryTranslation
                {
                    Language = languages[input.LanguageCode],
                    Name = input.Name,
                    Description = input.Description,
                };
            },
            update: (existing, input) =>
            {
                Validate(input);
                existing.Name = input.Name;
                existing.Description = input.Description;
            });

        categoryRepository.Update(category);
        await uow.SaveChangesAsync(ct);
        return true;
    }

    public Task<bool> DeleteCategoryAsync(int categoryId, CancellationToken ct = default)
        => uow.ExecuteInTransactionAsync(_ => categoryRepository.DeleteByIdAsync(categoryId), ct);

    public async Task<DashboardMetrics> GetDashboardMetricsAsync(int projectId, string languageCode, string defaultLanguageCode, int topCount = 5)
    {
        var categories = await categoryRepository.ReadTopCategoriesAsync(projectId, topCount, languageCode, defaultLanguageCode);
        var ideas = (await categoryRepository.ReadTopIdeasAsync(projectId, topCount)).Select(i => i with
        {
            Description = i.Description.Length > 100 ? i.Description[..100] + "…" : i.Description
        }).ToList();

        return new DashboardMetrics(categories, ideas);
    }

    public async Task<List<IdeaFeedItem>> GetIdeaFeedAsync(int projectId, string languageCode, int count = 10)
    {
        var ideas = await categoryRepository.ReadLatestIdeasAsync(projectId, count);
        return ideas.Select(i => new IdeaFeedItem(
            IdeaId: i.IdeaId,
            Description: i.Description.Length > 80 ? i.Description[..80] + "…" : i.Description,
            CategoryName: ResolveCategoryName(i.Category, languageCode),
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
        GetIdeasPagedAsync(int projectId, string search, int? categoryId, ModerationStatus? status, int page, string languageCode, int pageSize = 20)
    {
        var (ideas, total) = await ideaRepository.ReadIdeasPagedAsync(
            projectId, search, categoryId, status, page, pageSize);

        var queue    = await categoryRepository.ReadModerationQueueAsync(projectId, categoryId);
        var approved = queue.GetValueOrDefault(ModerationStatus.Approved, 0);
        var pending  = queue.GetValueOrDefault(ModerationStatus.Unflagged, 0);
        var flagged  = queue.GetValueOrDefault(ModerationStatus.Flagged, 0);

        var items = ideas.Select(i => new IdeaFeedItem(
            IdeaId:        i.IdeaId,
            Description:   i.Description,
            CategoryName:  ResolveCategoryName(i.Category, languageCode),
            CategoryId:    i.Category.CategoryId,
            ReactionCount: i.Comments.OfType<IconComment>().Count(),
            CreatedAt:     i.CreatedAt,
            Status:        i.Status,
            ParticipantEmail: i.Participant?.Email,
            ImageUrl:      i.ImageUrl,
            LanguageCode:  i.Language?.Code
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

    private static string ResolveCategoryName(Category category, string languageCode)
    {
        if (category?.Translations == null) return string.Empty;
        return category.Translations.FirstOrDefault(t => t.Language?.Code == languageCode)?.Name
            ?? category.Translations.FirstOrDefault()?.Name
            ?? string.Empty;
    }

    private async Task<Dictionary<string, Domain.I18n.Entities.Language>> ValidateTranslationInputsAsync(
        ICollection<CategoryTranslationInput> translations, Project project)
    {
        if (translations.Count == 0)
            throw new LocalizedValidationException("Validation.Translation.AtLeastOne");

        var codes = translations.Select(t => t.LanguageCode).ToList();
        if (codes.Distinct().Count() != codes.Count)
            throw new LocalizedValidationException("Validation.Translation.OneRowPerLanguage");

        var supportedCodes = project?.SupportedLanguages?.Select(l => l.Code).ToHashSet() ?? new HashSet<string>();
        if (supportedCodes.Count > 0 && !supportedCodes.All(codes.Contains))
            throw new LocalizedValidationException("Validation.Category.AllProjectLanguagesNeedTranslation");

        var resolved = (await languageRepository.ReadByCodesForUpdateAsync(codes)).ToList();
        var byCode = resolved.ToDictionary(l => l.Code);
        var missing = codes.Where(c => !byCode.ContainsKey(c)).ToList();
        if (missing.Count > 0)
            throw new LocalizedValidationException("Validation.Language.UnknownCodes", string.Join(", ", missing));

        return byCode;
    }
}
