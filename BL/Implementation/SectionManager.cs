using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.DAL.Interfaces;
using SynapseEnqueteManagement.BL.Interfaces;
using SynapseEnqueteManagement.BL.Domain.I18n.Entities;

namespace SynapseEnqueteManagement.BL.Implementation;

public class SectionManager(
    ISectionRepository sectionRepository,
    IQuestionnaireRepository questionnaireRepository,
    ILanguageResolver languageResolver,
    IUnitOfWork uow) : ISectionManager
{
    public Task<IEnumerable<Section>> GetSectionsReadOnlyAsync(int projectId)
        => sectionRepository.ReadByProjectIdReadOnlyAsync(projectId);

    public async Task<Section> CreateSectionAsync(int projectId, IEnumerable<SectionTranslationInput> translations, CancellationToken ct = default)
    {
        var questionnaire = await questionnaireRepository.ReadByProjectIdForUpdateAsync(projectId);
        if (questionnaire is null) return null;

        var translationList = translations?.ToList() ?? new List<SectionTranslationInput>();
        var languages = await ValidateSectionTranslationInputsAsync(translationList);

        var section = new Section { Questionnaire = questionnaire };
        foreach (var input in translationList)
        {
            EntityValidator.ValidateProperty(new SectionTranslation(), input.Name, nameof(SectionTranslation.Name));
            EntityValidator.ValidateProperty(new SectionTranslation(), input.Description, nameof(SectionTranslation.Description));
            section.Translations.Add(new SectionTranslation
            {
                Language = languages[input.LanguageCode],
                Name = input.Name,
                Description = input.Description
            });
        }

        EntityValidator.Validate(section);
        var created = await sectionRepository.CreateAsync(section);
        await uow.SaveChangesAsync(ct);
        return created;
    }

    public async Task<bool> UpdateSectionAsync(int sectionId, IEnumerable<SectionTranslationInput> translations, CancellationToken ct = default)
    {
        var section = await sectionRepository.ReadByIdForUpdateAsync(sectionId);
        if (section is null) return false;

        var translationList = translations?.ToList() ?? new List<SectionTranslationInput>();
        var languages = await ValidateSectionTranslationInputsAsync(translationList);

        void Validate(SectionTranslationInput input)
        {
            EntityValidator.ValidateProperty(new SectionTranslation(), input.Name, nameof(SectionTranslation.Name));
            EntityValidator.ValidateProperty(new SectionTranslation(), input.Description, nameof(SectionTranslation.Description));
        }

        TranslationUpserter.Upsert(
            section.Translations,
            translationList,
            inputLanguageCode: i => i.LanguageCode,
            existingLanguageCode: t => t.Language.Code,
            create: input =>
            {
                Validate(input);
                return new SectionTranslation
                {
                    Language = languages[input.LanguageCode],
                    Name = input.Name,
                    Description = input.Description
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

    public Task<bool> DeleteSectionAsync(int sectionId, CancellationToken ct = default)
        => uow.ExecuteInTransactionAsync(_ => sectionRepository.DeleteByIdAsync(sectionId), ct);

    private Task<IReadOnlyDictionary<string, Language>> ValidateSectionTranslationInputsAsync(
        ICollection<SectionTranslationInput> translations)
        => languageResolver.ResolveForTranslationsAsync(
            translations.Select(t => t.LanguageCode).ToList());
}
