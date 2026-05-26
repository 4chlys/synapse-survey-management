using System.ComponentModel.DataAnnotations;
using SynapseEnqueteManagement.BL.Domain.I18n.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;
using SynapseEnqueteManagement.BL.Exceptions;
using SynapseEnqueteManagement.BL.Helpers;
using SynapseEnqueteManagement.DAL.Interfaces;
using SynapseEnqueteManagement.BL.Interfaces;

namespace SynapseEnqueteManagement.BL.Implementation;

public class ProjectManager(
    IProjectRepository projectRepository,
    IEmojiManager emojiManager,
    IProjectTranslationValidator translationValidator,
    IProjectActivationValidator activationValidator,
    IShortFormService shortFormService,
    IProjectDuplicationService projectDuplicationService,
    IUnitOfWork uow) : IProjectManager
{
    public Task<Project> GetProjectByIdWithIntroductionReadOnlyAsync(int projectId)
        => projectRepository.ReadByIdWithIntroductionReadOnlyAsync(projectId);

    public Task<Project> GetProjectByIdReadOnlyAsync(int projectId)
        => projectRepository.ReadByIdReadOnlyAsync(projectId);

    public Task<Project> GetProjectByIdForUpdateAsync(int projectId)
        => projectRepository.ReadByIdForUpdateAsync(projectId);

    public Task<Project> GetProjectByIdWithQuestionnaireForUpdateAsync(int projectId)
        => projectRepository.ReadByIdWithQuestionnaireForUpdateAsync(projectId);

    public Task<Project> GetProjectByIdWithTranslationsReadOnlyAsync(int projectId)
        => projectRepository.ReadByIdWithTranslationsReadOnlyAsync(projectId);

    public async Task<IEnumerable<Emoji>> GetEmojisForProjectAsync(int projectId)
    {
        var project = await projectRepository.ReadByIdWithEmojisReadOnlyAsync(projectId);
        return project?.Emojis ?? Enumerable.Empty<Emoji>();
    }

    public Task<Project> GetProjectByShortFormAsync(string shortForm)
        => projectRepository.ReadByShortFormReadOnlyAsync(shortForm);

    public async Task<Project> CreateProjectAsync(
        IEnumerable<ProjectTranslationInput> translations,
        IEnumerable<IntroductionTranslationInput> introductions,
        string defaultLanguageCode,
        IEnumerable<string> supportedLanguageCodes,
        string imageUrl, FormType formType,
        int? minAge, int? maxAge, bool requestContactInfo, int platformId,
        string shortForm = null, int ideaPageSize = 5,
        bool extraIdeasReplaceExisting = false, int maxExtraIdeaRequests = 3,
        bool allowCrossLanguageIdeas = false,
        ProjectTheme theme = ProjectTheme.Minimal,
        string primaryColor = null,
        string customCss = null)
    {
        var translationList = translations?.ToList() ?? new List<ProjectTranslationInput>();
        var supportedList = supportedLanguageCodes?.Distinct().ToList() ?? new List<string>();
        var introductionList = introductions?.ToList() ?? new List<IntroductionTranslationInput>();

        var languages = await translationValidator.ValidateAsync(
            translationList, supportedList, defaultLanguageCode, introductionList);

        var defaultName = translationList.First(t => t.LanguageCode == defaultLanguageCode).Name;
        var unique = await shortFormService.ResolveAsync(shortForm, defaultName);

        var project = new Project
        {
            ShortForm = unique,
            ProjectPlatformId = platformId,
            ImageUrl = imageUrl,
            FormType = formType,
            MinAge = minAge,
            MaxAge = maxAge,
            RequestContactInfo = requestContactInfo,
            AllowCrossLanguageIdeas = allowCrossLanguageIdeas,
            IdeaPageSize = ideaPageSize,
            ExtraIdeasReplaceExisting = extraIdeasReplaceExisting,
            MaxExtraIdeaRequests = maxExtraIdeaRequests,
            ProjectState = ProjectState.Draft,
            Questionnaire = new Questionnaire(),
            CreatedAt = DateTime.UtcNow,
            DefaultLanguage = languages[defaultLanguageCode],
            Theme = theme,
            PrimaryColor = primaryColor,
            CustomCss = customCss,
        };

        foreach (var code in supportedList)
            project.SupportedLanguages.Add(languages[code]);

        foreach (var t in translationList)
        {
            EntityValidator.ValidateProperty(new ProjectTranslation(), t.Name, nameof(ProjectTranslation.Name));
            EntityValidator.ValidateProperty(new ProjectTranslation(), t.Topic, nameof(ProjectTranslation.Topic));
            project.Translations.Add(new ProjectTranslation
            {
                Language = languages[t.LanguageCode],
                Name = t.Name,
                Topic = t.Topic,
            });
        }

        ValidateProjectProperties(project, imageUrl, minAge, maxAge, primaryColor, customCss);

        var nonEmptyIntros = introductionList.Where(i => !string.IsNullOrEmpty(i.Text)).ToList();
        if (nonEmptyIntros.Count > 0)
        {
            var intro = new Introduction();
            foreach (var introInput in nonEmptyIntros)
            {
                EntityValidator.ValidateProperty(new IntroductionTranslation(), introInput.Text, nameof(IntroductionTranslation.Text));
                intro.Translations.Add(new IntroductionTranslation
                {
                    Language = languages[introInput.LanguageCode],
                    Text = introInput.Text,
                });
            }
            project.Introduction = intro;
        }

        var created = await projectRepository.CreateAsync(project);
        await uow.SaveChangesAsync();
        return created;
    }

    public async Task<Project> UpdateProjectAsync(int projectId,
        IEnumerable<ProjectTranslationInput> translations,
        IEnumerable<IntroductionTranslationInput> introductions,
        string defaultLanguageCode,
        IEnumerable<string> supportedLanguageCodes,
        string imageUrl, FormType formType,
        int? minAge, int? maxAge, bool requestContactInfo,
        string shortForm = null, int ideaPageSize = 5,
        bool extraIdeasReplaceExisting = false, int maxExtraIdeaRequests = 3,
        bool allowCrossLanguageIdeas = false,
        ProjectTheme theme = ProjectTheme.Minimal,
        string primaryColor = null,
        string customCss = null)
    {
        var project = await projectRepository.ReadByIdWithTranslationsAndLanguagesForUpdateAsync(projectId);
        if (project is null)
            throw new ArgumentException($"Project {projectId} not found.");

        var translationList = translations?.ToList() ?? new List<ProjectTranslationInput>();
        var supportedList = supportedLanguageCodes?.Distinct().ToList() ?? new List<string>();
        var introductionList = introductions?.ToList() ?? new List<IntroductionTranslationInput>();

        var languages = await translationValidator.ValidateAsync(
            translationList, supportedList, defaultLanguageCode, introductionList);

        if (shortForm != null && shortForm != project.ShortForm)
        {
            if (project.ProjectState != ProjectState.Draft)
                throw new LocalizedValidationException("Validation.ShortForm.CannotChangeAfterPublish");

            project.ShortForm = await shortFormService.ResolveAsync(shortForm, defaultName: null, excludeProjectId: projectId);
        }

        ValidateProjectProperties(project, imageUrl, minAge, maxAge, primaryColor, customCss);

        project.ImageUrl = imageUrl;
        project.FormType = formType;
        project.MinAge = minAge;
        project.MaxAge = maxAge;
        project.RequestContactInfo = requestContactInfo;
        project.AllowCrossLanguageIdeas = allowCrossLanguageIdeas;
        project.IdeaPageSize = ideaPageSize;
        project.ExtraIdeasReplaceExisting = extraIdeasReplaceExisting;
        project.MaxExtraIdeaRequests = maxExtraIdeaRequests;
        project.DefaultLanguage = languages[defaultLanguageCode];
        project.Theme = theme;
        project.PrimaryColor = primaryColor;
        project.CustomCss = customCss;

        ReconcileSupportedLanguages(project, supportedList, languages);

        void ValidateProjectTranslation(ProjectTranslationInput input)
        {
            EntityValidator.ValidateProperty(new ProjectTranslation(), input.Name, nameof(ProjectTranslation.Name));
            EntityValidator.ValidateProperty(new ProjectTranslation(), input.Topic, nameof(ProjectTranslation.Topic));
        }

        TranslationUpserter.Upsert(
            project.Translations,
            translationList,
            inputLanguageCode: i => i.LanguageCode,
            existingLanguageCode: t => t.Language.Code,
            create: input =>
            {
                ValidateProjectTranslation(input);
                return new ProjectTranslation
                {
                    Language = languages[input.LanguageCode],
                    Name = input.Name,
                    Topic = input.Topic,
                };
            },
            update: (existing, input) =>
            {
                ValidateProjectTranslation(input);
                existing.Name = input.Name;
                existing.Topic = input.Topic;
            });

        var nonEmptyIntros = introductionList.Where(i => !string.IsNullOrEmpty(i.Text)).ToList();
        if (nonEmptyIntros.Count == 0)
        {
            project.Introduction = null;
        }
        else
        {
            project.Introduction ??= new Introduction();

            TranslationUpserter.Upsert(
                project.Introduction.Translations,
                nonEmptyIntros,
                inputLanguageCode: i => i.LanguageCode,
                existingLanguageCode: t => t.Language.Code,
                create: introInput =>
                {
                    EntityValidator.ValidateProperty(new IntroductionTranslation(), introInput.Text, nameof(IntroductionTranslation.Text));
                    return new IntroductionTranslation
                    {
                        Language = languages[introInput.LanguageCode],
                        Text = introInput.Text,
                    };
                },
                update: (existing, introInput) =>
                {
                    EntityValidator.ValidateProperty(new IntroductionTranslation(), introInput.Text, nameof(IntroductionTranslation.Text));
                    existing.Text = introInput.Text;
                });
        }

        await uow.SaveChangesAsync();
        return project;
    }

    public Task<IEnumerable<Project>> GetAllProjectsAsync(int? platformId = null)
        => projectRepository.ReadAllWithPlatformReadOnlyAsync(platformId);

    public async Task IncrementViewCountAsync(int projectId)
    {
        var project = await projectRepository.ReadByIdForUpdateAsync(projectId);
        if (project is null) return;
        project.ViewCount++;
        await uow.SaveChangesAsync();
    }

    public async Task<Project> UpdateProjectStateAsync(int projectId, ProjectState newState)
    {
        if (newState == ProjectState.Active)
        {
            var fullGraph = await projectRepository.ReadByIdForActivationReadOnlyAsync(projectId);
            if (fullGraph is null)
                throw new ArgumentException($"Project {projectId} not found.");

            var problems = activationValidator.Validate(fullGraph);
            if (problems.Count > 0)
                throw new ProjectActivationException(problems);
        }

        var project = await projectRepository.ReadByIdForUpdateAsync(projectId);
        if (project is null)
            throw new ArgumentException($"Project {projectId} not found.");

        project.ProjectState = newState;
        await uow.SaveChangesAsync();
        return project;
    }

    public async Task DeleteProjectAsync(int projectId)
    {
        // Idempotent: a DELETE on an already-removed project is a no-op, not an error.
        // Guards against double-submits where the second request would otherwise 500.
        var deleted = await projectRepository.DeleteWithRelationsAsync(projectId);
        if (!deleted) return;

        await uow.SaveChangesAsync();
    }

    public async Task UpdateLastWizardStepAsync(int projectId, int step)
    {
        var project = await projectRepository.ReadByIdForUpdateAsync(projectId)
            ?? throw new ArgumentException($"Project {projectId} not found.", nameof(projectId));
        project.LastWizardStep = Math.Clamp(step, 1, 3);
        await uow.SaveChangesAsync();
    }

    public async Task<Project> DuplicateProjectAsync(int sourceProjectId,
        IEnumerable<ProjectTranslationInput> translations,
        int platformId, string newShortForm = null, CancellationToken ct = default)
    {
        var src = await projectRepository.ReadByIdForDuplicationReadOnlyAsync(sourceProjectId);
        if (src is null)
            throw new KeyNotFoundException($"Project {sourceProjectId} not found.");

        if (src.ProjectPlatformId != platformId)
            throw new UnauthorizedAccessException("Source project belongs to a different platform.");

        var translationList = translations?.ToList() ?? new List<ProjectTranslationInput>();
        var supportedCodes = src.SupportedLanguages.Select(l => l.Code).ToList();
        var defaultCode = src.DefaultLanguage?.Code
            ?? throw new InvalidOperationException("Source project has no default language.");

        var languages = await translationValidator.ValidateAsync(
            translationList, supportedCodes, defaultCode, Array.Empty<IntroductionTranslationInput>());

        var defaultName = translationList.First(t => t.LanguageCode == defaultCode).Name;
        var uniqueShortForm = await shortFormService.ResolveAsync(newShortForm, defaultName);

        var copy = await projectDuplicationService.BuildCloneAsync(src, translationList, languages, uniqueShortForm, platformId);

        await projectRepository.CreateAsync(copy);
        await uow.SaveChangesAsync(ct);
        return copy;
    }

    public async Task<bool> EnableEmojiForProjectAsync(int projectId, int emojiId, CancellationToken ct = default)
    {
        var project = await projectRepository.ReadByIdWithEmojisForUpdateAsync(projectId);
        if (project == null) return false;

        var emoji = await emojiManager.GetByIdForUpdateAsync(emojiId);
        if (emoji == null) return false;

        if (project.Emojis.Any(e => e.EmojiId == emojiId)) return true;

        project.Emojis.Add(emoji);
        await uow.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> DisableEmojiForProjectAsync(int projectId, int emojiId, CancellationToken ct = default)
    {
        var project = await projectRepository.ReadByIdWithEmojisForUpdateAsync(projectId);
        if (project == null) return false;

        var emoji = project.Emojis.FirstOrDefault(e => e.EmojiId == emojiId);
        if (emoji == null) return false;

        project.Emojis.Remove(emoji);
        await uow.SaveChangesAsync(ct);
        return true;
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private static void ValidateProjectProperties(
        Project project,
        string imageUrl,
        int? minAge,
        int? maxAge,
        string primaryColor,
        string customCss)
    {
        EntityValidator.ValidateProperty(project, imageUrl, nameof(Project.ImageUrl));
        EntityValidator.ValidateProperty(project, minAge, nameof(Project.MinAge));
        EntityValidator.ValidateProperty(project, maxAge, nameof(Project.MaxAge));
        EntityValidator.ValidateProperty(project, primaryColor, nameof(Project.PrimaryColor));
        EntityValidator.ValidateProperty(project, customCss, nameof(Project.CustomCss));
        ProjectThemeValidator.ValidatePrimaryColor(primaryColor);
        ProjectThemeValidator.ValidateCustomCss(customCss);
        ProjectThemeValidator.ValidateAgeRange(minAge, maxAge);
    }

    private static void ReconcileSupportedLanguages(
        Project project,
        IReadOnlyCollection<string> desiredCodes,
        IReadOnlyDictionary<string, Language> languages)
    {
        var desiredSet = desiredCodes.ToHashSet();
        foreach (var existing in project.SupportedLanguages.Where(l => !desiredSet.Contains(l.Code)).ToList())
            project.SupportedLanguages.Remove(existing);
        foreach (var code in desiredCodes)
            if (!project.SupportedLanguages.Any(l => l.Code == code))
                project.SupportedLanguages.Add(languages[code]);
    }
}
