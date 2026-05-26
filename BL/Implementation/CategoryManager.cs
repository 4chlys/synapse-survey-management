using System.ComponentModel.DataAnnotations;
using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Stats;
using SynapseEnqueteManagement.BL.Exceptions;
using SynapseEnqueteManagement.BL.Interfaces;
using SynapseEnqueteManagement.DAL.Interfaces;

namespace SynapseEnqueteManagement.BL.Implementation;

public class CategoryManager(
    ICategoryRepository categoryRepository,
    ILanguageResolver languageResolver,
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

        await uow.SaveChangesAsync(ct);
        return true;
    }

    public Task<bool> DeleteCategoryAsync(int categoryId, CancellationToken ct = default)
        => uow.ExecuteInTransactionAsync(_ => categoryRepository.DeleteByIdAsync(categoryId), ct);

    public Task<List<CategoryMetric>> GetTopCategoriesAsync(int projectId, int count, string languageCode, string defaultLanguageCode)
        => categoryRepository.ReadTopCategoriesAsync(projectId, count, languageCode, defaultLanguageCode);

    private Task<IReadOnlyDictionary<string, Domain.I18n.Entities.Language>> ValidateTranslationInputsAsync(
        ICollection<CategoryTranslationInput> translations, Project project)
    {
        var codes = translations.Select(t => t.LanguageCode).ToList();

        var supportedCodes = project?.SupportedLanguages?.Select(l => l.Code).ToHashSet() ?? new HashSet<string>();
        if (supportedCodes.Count > 0 && !supportedCodes.All(codes.Contains))
            throw new LocalizedValidationException("Validation.Category.AllProjectLanguagesNeedTranslation");

        return languageResolver.ResolveForTranslationsAsync(codes);
    }
}
