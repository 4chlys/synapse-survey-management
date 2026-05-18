using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using SynapseEnqueteManagement.BL.Domain.I18n.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;
using SynapseEnqueteManagement.DAL.Interfaces;
using SynapseEnqueteManagement.BL.Interfaces;

namespace SynapseEnqueteManagement.BL.Implementation;

public class ProjectManager(
    IProjectRepository projectRepository,
    IEmojiRepository emojiRepository,
    ILanguageRepository languageRepository,
    IShortFormService shortFormService,
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
        string imageUrl, FormType formType, AiAssistanceType aiAssistance,
        int? minAge, int? maxAge, bool requestContactInfo, int platformId,
        string shortForm = null, int ideaPageSize = 5,
        bool extraIdeasReplaceExisting = false, int maxExtraIdeaRequests = 3,
        bool allowCrossLanguageIdeas = false,
        ProjectTheme theme = ProjectTheme.Default,
        string primaryColor = null,
        string customCss = null)
    {
        var translationList = translations?.ToList() ?? new List<ProjectTranslationInput>();
        var supportedList = supportedLanguageCodes?.Distinct().ToList() ?? new List<string>();
        var introductionList = introductions?.ToList() ?? new List<IntroductionTranslationInput>();

        var languages = await ValidateTranslationInputsAsync(
            translationList, supportedList, defaultLanguageCode, introductionList);

        var defaultName = translationList.First(t => t.LanguageCode == defaultLanguageCode).Name;
        var candidate = string.IsNullOrWhiteSpace(shortForm)
            ? shortFormService.Normalize(defaultName)
            : shortForm;

        if (!shortFormService.IsValid(candidate))
            throw new ValidationException($"'{candidate}' is not a valid shortform. Use lowercase letters, digits, and single hyphens only.");
        if (shortFormService.IsReserved(candidate))
            throw new ValidationException($"'{candidate}' is a reserved shortform and cannot be used.");

        var unique = await shortFormService.EnsureUniqueAsync(candidate);

        var project = new Project
        {
            ShortForm = unique,
            ProjectPlatformId = platformId,
            ImageUrl = imageUrl,
            FormType = formType,
            AiAssistance = aiAssistance,
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

        EntityValidator.ValidateProperty(project, imageUrl, nameof(Project.ImageUrl));
        EntityValidator.ValidateProperty(project, minAge, nameof(Project.MinAge));
        EntityValidator.ValidateProperty(project, maxAge, nameof(Project.MaxAge));
        EntityValidator.ValidateProperty(project, primaryColor, nameof(Project.PrimaryColor));
        EntityValidator.ValidateProperty(project, customCss, nameof(Project.CustomCss));
        if (!string.IsNullOrEmpty(primaryColor) &&
            !Regex.IsMatch(primaryColor, @"^#[0-9A-Fa-f]{6}$"))
            throw new ValidationException(
                "Invalid color code. Use a hex value (e.g. #FF5733).");
        if (!string.IsNullOrEmpty(customCss) &&
            Regex.IsMatch(customCss,
                @"<\s*/?\s*(script|style)\b|expression\s*\(|url\s*\(\s*['""]?\s*javascript:|@import\b|behavior\s*:",
                RegexOptions.IgnoreCase))
            throw new ValidationException(
                "Custom CSS contains disallowed content (scripts, expression(), javascript: urls, @import, or behavior).");
        if (minAge.HasValue && maxAge.HasValue && minAge > maxAge)
            throw new ValidationException("MinAge must not be greater than MaxAge.");

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
        string imageUrl, FormType formType, AiAssistanceType aiAssistance,
        int? minAge, int? maxAge, bool requestContactInfo,
        string shortForm = null, int ideaPageSize = 5,
        bool extraIdeasReplaceExisting = false, int maxExtraIdeaRequests = 3,
        bool allowCrossLanguageIdeas = false,
        ProjectTheme theme = ProjectTheme.Default,
        string primaryColor = null,
        string customCss = null)
    {
        var project = await projectRepository.ReadByIdWithTranslationsAndLanguagesForUpdateAsync(projectId);
        if (project is null)
            throw new ArgumentException($"Project {projectId} not found.");

        var translationList = translations?.ToList() ?? new List<ProjectTranslationInput>();
        var supportedList = supportedLanguageCodes?.Distinct().ToList() ?? new List<string>();
        var introductionList = introductions?.ToList() ?? new List<IntroductionTranslationInput>();

        var languages = await ValidateTranslationInputsAsync(
            translationList, supportedList, defaultLanguageCode, introductionList);

        if (shortForm != null && shortForm != project.ShortForm)
        {
            if (project.ProjectState != ProjectState.Draft)
                throw new ValidationException("ShortForm cannot be changed after the project has been published.");
            if (!shortFormService.IsValid(shortForm))
                throw new ValidationException($"'{shortForm}' is not a valid shortform. Use lowercase letters, digits, and single hyphens only.");
            if (shortFormService.IsReserved(shortForm))
                throw new ValidationException($"'{shortForm}' is a reserved shortform and cannot be used.");

            project.ShortForm = await shortFormService.EnsureUniqueAsync(shortForm, excludeProjectId: projectId);
        }

        EntityValidator.ValidateProperty(project, imageUrl, nameof(Project.ImageUrl));
        EntityValidator.ValidateProperty(project, minAge, nameof(Project.MinAge));
        EntityValidator.ValidateProperty(project, maxAge, nameof(Project.MaxAge));
        EntityValidator.ValidateProperty(project, primaryColor, nameof(Project.PrimaryColor));
        EntityValidator.ValidateProperty(project, customCss, nameof(Project.CustomCss));
        if (!string.IsNullOrEmpty(primaryColor) &&
            !Regex.IsMatch(primaryColor, @"^#[0-9A-Fa-f]{6}$"))
            throw new ValidationException(
                "Invalid color code. Use a hex value (e.g. #FF5733).");
        if (!string.IsNullOrEmpty(customCss) &&
            Regex.IsMatch(customCss,
                @"<\s*/?\s*(script|style)\b|expression\s*\(|url\s*\(\s*['""]?\s*javascript:|@import\b|behavior\s*:",
                RegexOptions.IgnoreCase))
            throw new ValidationException(
                "Custom CSS contains disallowed content (scripts, expression(), javascript: urls, @import, or behavior).");
        if (minAge.HasValue && maxAge.HasValue && minAge > maxAge)
            throw new ValidationException("MinAge must not be greater than MaxAge.");

        project.ImageUrl = imageUrl;
        project.FormType = formType;
        project.AiAssistance = aiAssistance;
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

        var supportedSet = supportedList.ToHashSet();
        foreach (var existing in project.SupportedLanguages.Where(l => !supportedSet.Contains(l.Code)).ToList())
            project.SupportedLanguages.Remove(existing);
        foreach (var code in supportedList)
            if (!project.SupportedLanguages.Any(l => l.Code == code))
                project.SupportedLanguages.Add(languages[code]);

        var inputByCode = translationList.ToDictionary(t => t.LanguageCode);
        foreach (var stale in project.Translations.Where(t => !inputByCode.ContainsKey(t.Language.Code)).ToList())
            project.Translations.Remove(stale);

        foreach (var input in translationList)
        {
            EntityValidator.ValidateProperty(new ProjectTranslation(), input.Name, nameof(ProjectTranslation.Name));
            EntityValidator.ValidateProperty(new ProjectTranslation(), input.Topic, nameof(ProjectTranslation.Topic));

            var existing = project.Translations.FirstOrDefault(t => t.Language.Code == input.LanguageCode);
            if (existing is null)
            {
                project.Translations.Add(new ProjectTranslation
                {
                    Language = languages[input.LanguageCode],
                    Name = input.Name,
                    Topic = input.Topic,
                });
            }
            else
            {
                existing.Name = input.Name;
                existing.Topic = input.Topic;
            }
        }

        var nonEmptyIntros = introductionList.Where(i => !string.IsNullOrEmpty(i.Text)).ToList();
        if (nonEmptyIntros.Count == 0)
        {
            project.Introduction = null;
        }
        else
        {
            project.Introduction ??= new Introduction();

            var inputByCodes = nonEmptyIntros.ToDictionary(i => i.LanguageCode);
            foreach (var stale in project.Introduction.Translations
                         .Where(t => !inputByCodes.ContainsKey(t.Language.Code)).ToList())
                project.Introduction.Translations.Remove(stale);

            foreach (var introInput in nonEmptyIntros)
            {
                EntityValidator.ValidateProperty(new IntroductionTranslation(), introInput.Text, nameof(IntroductionTranslation.Text));
                var existing = project.Introduction.Translations.FirstOrDefault(t => t.Language.Code == introInput.LanguageCode);
                if (existing is null)
                {
                    project.Introduction.Translations.Add(new IntroductionTranslation
                    {
                        Language = languages[introInput.LanguageCode],
                        Text = introInput.Text,
                    });
                }
                else
                {
                    existing.Text = introInput.Text;
                }
            }
        }

        projectRepository.Update(project);
        await uow.SaveChangesAsync();
        return project;
    }

    private async Task<Dictionary<string, Language>> ValidateTranslationInputsAsync(
        ICollection<ProjectTranslationInput> translations,
        ICollection<string> supportedLanguageCodes,
        string defaultLanguageCode,
        ICollection<IntroductionTranslationInput> introductions)
    {
        if (string.IsNullOrWhiteSpace(defaultLanguageCode))
            throw new ValidationException("DefaultLanguageCode is required.");
        if (supportedLanguageCodes.Count == 0)
            throw new ValidationException("At least one supported language is required.");
        if (!supportedLanguageCodes.Contains(defaultLanguageCode))
            throw new ValidationException("DefaultLanguageCode must be one of the SupportedLanguageCodes.");

        var translationCodes = translations.Select(t => t.LanguageCode).ToList();
        if (translationCodes.Distinct().Count() != translationCodes.Count)
            throw new ValidationException("Translations must contain a single row per language.");
        if (!supportedLanguageCodes.All(translationCodes.Contains))
            throw new ValidationException("Every supported language must have a translation row.");

        var introByCode = introductions.GroupBy(i => i.LanguageCode).ToList();
        if (introByCode.Any(g => g.Count() > 1))
            throw new ValidationException("Introductions must contain a single row per language.");

        var allCodes = supportedLanguageCodes
            .Concat(translationCodes)
            .Concat(introductions.Select(i => i.LanguageCode))
            .Distinct()
            .ToList();
        var resolved = (await languageRepository.ReadByCodesForUpdateAsync(allCodes)).ToList();
        if (resolved.Any(l => !l.IsActive))
            throw new ValidationException("Inactive languages cannot be assigned to a project.");

        var byCode = resolved.ToDictionary(l => l.Code);
        var missing = allCodes.Where(c => !byCode.ContainsKey(c)).ToList();
        if (missing.Count > 0)
            throw new ValidationException($"Unknown language code(s): {string.Join(", ", missing)}.");

        return byCode;
    }

    public Task<IEnumerable<Project>> GetAllProjectsAsync(int? platformId = null)
        => projectRepository.ReadAllWithPlatformReadOnlyAsync(platformId);

    public async Task IncrementViewCountAsync(int projectId)
    {
        var project = await projectRepository.ReadByIdForUpdateAsync(projectId);
        if (project is null) return;
        project.ViewCount++;
        projectRepository.Update(project);
        await uow.SaveChangesAsync();
    }

    public Task<IEnumerable<Emoji>> GetAllEmojisAsync()
        => emojiRepository.ReadAllReadOnlyAsync();

    public async Task<Project> UpdateProjectStateAsync(int projectId, ProjectState newState)
    {
        var project = await projectRepository.ReadByIdForUpdateAsync(projectId);
        if (project is null)
            throw new ArgumentException($"Project {projectId} not found.");

        project.ProjectState = newState;
        projectRepository.Update(project);
        await uow.SaveChangesAsync();
        return project;
    }

    public async Task DeleteProjectAsync(int projectId)
    {
        var deleted = await projectRepository.DeleteWithRelationsAsync(projectId);
        if (!deleted)
            throw new KeyNotFoundException($"Project {projectId} not found.");

        await uow.SaveChangesAsync();
    }

    public async Task UpdateAiAssistanceModeAsync(int projectId, AiAssistanceType mode)
    {
        var project = await projectRepository.ReadByIdForUpdateAsync(projectId)
            ?? throw new ArgumentException($"Project {projectId} not found.", nameof(projectId));
        project.AiAssistance = mode;
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

        var languages = await ValidateTranslationInputsAsync(
            translationList, supportedCodes, defaultCode, Array.Empty<IntroductionTranslationInput>());

        var defaultName = translationList.First(t => t.LanguageCode == defaultCode).Name;
        var candidate = string.IsNullOrWhiteSpace(newShortForm)
            ? shortFormService.Normalize(defaultName)
            : newShortForm;

        if (!shortFormService.IsValid(candidate))
            throw new ValidationException($"'{candidate}' is not a valid shortform. Use lowercase letters, digits, and single hyphens only.");
        if (shortFormService.IsReserved(candidate))
            throw new ValidationException($"'{candidate}' is a reserved shortform and cannot be used.");

        var uniqueShortForm = await shortFormService.EnsureUniqueAsync(candidate);

        var copy = new Project
        {
            ShortForm = uniqueShortForm,
            ImageUrl = src.ImageUrl,
            FormType = src.FormType,
            AiAssistance = src.AiAssistance,
            MinAge = src.MinAge,
            MaxAge = src.MaxAge,
            RequestContactInfo = src.RequestContactInfo,
            AllowCrossLanguageIdeas = src.AllowCrossLanguageIdeas,
            IdeaPageSize = src.IdeaPageSize,
            ExtraIdeasReplaceExisting = src.ExtraIdeasReplaceExisting,
            MaxExtraIdeaRequests = src.MaxExtraIdeaRequests,
            CommentPageSize = src.CommentPageSize,
            ProjectPlatformId = platformId,
            ProjectState = ProjectState.Draft,
            CreatedAt = DateTime.UtcNow,
            ViewCount = 0,
            DefaultLanguage = languages[defaultCode],
            Theme = src.Theme,
            PrimaryColor = src.PrimaryColor,
            CustomCss = src.CustomCss,
        };

        foreach (var code in supportedCodes)
            copy.SupportedLanguages.Add(languages[code]);

        foreach (var t in translationList)
            copy.Translations.Add(new ProjectTranslation
            {
                Language = languages[t.LanguageCode],
                Name = t.Name,
                Topic = t.Topic,
            });

        if (src.Introduction is not null && src.Introduction.Translations.Count > 0)
        {
            var introCopy = new Introduction();
            foreach (var t in src.Introduction.Translations)
                introCopy.Translations.Add(new IntroductionTranslation { Language = languages[t.Language.Code], Text = t.Text });
            copy.Introduction = introCopy;
        }

        copy.Questionnaire = new Questionnaire();

        if (src.Questionnaire is not null)
        {
            var sectionMap = new Dictionary<int, Section>();
            var questionMap = new Dictionary<int, Question>();

            foreach (var srcSection in src.Questionnaire.Sections)
            {
                var newSection = new Section();
                foreach (var t in srcSection.Translations)
                    newSection.Translations.Add(new SectionTranslation { Language = languages[t.Language.Code], Name = t.Name, Description = t.Description });
                sectionMap[srcSection.SectionId] = newSection;
                copy.Questionnaire.Sections.Add(newSection);
            }

            foreach (var srcQ in src.Questionnaire.Questions)
            {
                var newQ = new Question
                {
                    QuestionFormat = srcQ.QuestionFormat,
                    IsMandatory = srcQ.IsMandatory,
                    AnswerChoiceCount = srcQ.AnswerChoiceCount,
                    ChoiceIndex = srcQ.ChoiceIndex,
                    Position = srcQ.Position,
                    Section = srcQ.Section is not null ? sectionMap[srcQ.Section.SectionId] : null,
                };
                foreach (var t in srcQ.Translations)
                    newQ.Translations.Add(new QuestionTranslation { Language = languages[t.Language.Code], Text = t.Text, AnswerChoices = t.AnswerChoices.ToList(), MediaText = t.MediaText });
                foreach (var ea in srcQ.EndingAnswers)
                {
                    var newEa = new EndingAnswer { Question = newQ };
                    foreach (var t in ea.Translations)
                        newEa.Translations.Add(new EndingAnswerTranslation { Language = languages[t.Language.Code], Value = t.Value });
                    newQ.EndingAnswers.Add(newEa);
                }
                questionMap[srcQ.QuestionId] = newQ;
                copy.Questionnaire.Questions.Add(newQ);
            }

            foreach (var srcQ in src.Questionnaire.Questions)
            {
                if (srcQ.ParentQuestion is not null && questionMap.TryGetValue(srcQ.ParentQuestion.QuestionId, out var newParent))
                    questionMap[srcQ.QuestionId].ParentQuestion = newParent;
            }
        }

        foreach (var srcCat in src.Categories)
        {
            var catCopy = new Category();
            foreach (var t in srcCat.Translations)
                catCopy.Translations.Add(new CategoryTranslation { Language = languages[t.Language.Code], Name = t.Name, Description = t.Description });
            copy.Categories.Add(catCopy);
        }

        foreach (var srcEmoji in src.Emojis)
        {
            var tracked = await emojiRepository.ReadByIdForUpdateAsync(srcEmoji.EmojiId);
            if (tracked is not null)
                copy.Emojis.Add(tracked);
        }

        await projectRepository.CreateAsync(copy);
        await uow.SaveChangesAsync(ct);
        return copy;
    }

    public async Task<bool> EnableEmojiForProjectAsync(int projectId, int emojiId, CancellationToken ct = default)
    {
        var project = await projectRepository.ReadByIdWithEmojisForUpdateAsync(projectId);
        if (project == null) return false;

        var emoji = await emojiRepository.ReadByIdForUpdateAsync(emojiId);
        if (emoji == null) return false;

        if (project.Emojis.Any(e => e.EmojiId == emojiId)) return true;

        project.Emojis.Add(emoji);
        projectRepository.Update(project);
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
        projectRepository.Update(project);
        await uow.SaveChangesAsync(ct);
        return true;
    }
}
