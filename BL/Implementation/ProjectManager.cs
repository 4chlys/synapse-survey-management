using System.ComponentModel.DataAnnotations;
using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;
using SynapseEnqueteManagement.DAL.Interfaces;
using SynapseEnqueteManagement.BL.Interfaces;

namespace SynapseEnqueteManagement.BL.Implementation;

public class ProjectManager(
    IProjectRepository projectRepository,
    IEmojiRepository emojiRepository,
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

    public async Task<IEnumerable<Emoji>> GetEmojisForProjectAsync(int projectId)
    {
        var project = await projectRepository.ReadByIdWithEmojisReadOnlyAsync(projectId);
        return project?.Emojis ?? Enumerable.Empty<Emoji>();
    }

    public Task<Project> GetProjectByShortFormAsync(string shortForm)
        => projectRepository.ReadByShortFormReadOnlyAsync(shortForm);

    public async Task<Project> CreateProjectAsync(string name, string topic, string introduction, string imageUrl, FormType formType, AiAssistanceType aiAssistance, int? minAge, int? maxAge, bool requestContactInfo, int platformId, string shortForm = null, int ideaPageSize = 5, bool extraIdeasReplaceExisting = false, int maxExtraIdeaRequests = 3)
    {
        var candidate = string.IsNullOrWhiteSpace(shortForm)
            ? shortFormService.Normalize(name)
            : shortForm;

        if (!shortFormService.IsValid(candidate))
            throw new ValidationException($"'{candidate}' is not a valid shortform. Use lowercase letters, digits, and single hyphens only.");

        if (shortFormService.IsReserved(candidate))
            throw new ValidationException($"'{candidate}' is a reserved shortform and cannot be used.");

        var unique = await shortFormService.EnsureUniqueAsync(candidate);

        var project = new Project
        {
            Name = name,
            ShortForm = unique,
            ProjectPlatformId = platformId,
            Topic = topic,
            ImageUrl = imageUrl,
            FormType = formType,
            AiAssistance = aiAssistance,
            MinAge = minAge,
            MaxAge = maxAge,
            RequestContactInfo = requestContactInfo,
            IdeaPageSize = ideaPageSize,
            ExtraIdeasReplaceExisting = extraIdeasReplaceExisting,
            MaxExtraIdeaRequests = maxExtraIdeaRequests,
            ProjectState = ProjectState.Draft,
            Questionnaire = new Questionnaire(),
            CreatedAt = DateTime.UtcNow
        };
        EntityValidator.ValidateProperty(project, name, nameof(Project.Name));
        EntityValidator.ValidateProperty(project, topic, nameof(Project.Topic));
        EntityValidator.ValidateProperty(project, imageUrl, nameof(Project.ImageUrl));
        EntityValidator.ValidateProperty(project, minAge, nameof(Project.MinAge));
        EntityValidator.ValidateProperty(project, maxAge, nameof(Project.MaxAge));
        if (minAge.HasValue && maxAge.HasValue && minAge > maxAge)
            throw new ValidationException("MinAge must not be greater than MaxAge.");
        if (!string.IsNullOrEmpty(introduction))
        {
            EntityValidator.ValidateProperty(new Introduction(), introduction, nameof(Introduction.Text));
            project.Introduction = new Introduction { Text = introduction };
        }

        var created = await projectRepository.CreateAsync(project);
        await uow.SaveChangesAsync();
        return created;
    }

    public async Task<Project> UpdateProjectAsync(int projectId, string name, string topic, string introduction, string imageUrl, FormType formType, AiAssistanceType aiAssistance, int? minAge, int? maxAge, bool requestContactInfo, string shortForm = null, int ideaPageSize = 5, bool extraIdeasReplaceExisting = false, int maxExtraIdeaRequests = 3)
    {
        var project = await projectRepository.ReadByIdWithIntroductionForUpdateAsync(projectId);
        if (project is null)
            throw new ArgumentException($"Project {projectId} not found.");

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

        EntityValidator.ValidateProperty(project, name, nameof(Project.Name));
        EntityValidator.ValidateProperty(project, topic, nameof(Project.Topic));
        EntityValidator.ValidateProperty(project, imageUrl, nameof(Project.ImageUrl));
        EntityValidator.ValidateProperty(project, minAge, nameof(Project.MinAge));
        EntityValidator.ValidateProperty(project, maxAge, nameof(Project.MaxAge));
        if (minAge.HasValue && maxAge.HasValue && minAge > maxAge)
            throw new ValidationException("MinAge must not be greater than MaxAge.");
        project.Name = name;
        project.Topic = topic;
        project.ImageUrl = imageUrl;
        project.FormType = formType;
        project.AiAssistance = aiAssistance;
        project.MinAge = minAge;
        project.MaxAge = maxAge;
        project.RequestContactInfo = requestContactInfo;
        project.IdeaPageSize = ideaPageSize;
        project.ExtraIdeasReplaceExisting = extraIdeasReplaceExisting;
        project.MaxExtraIdeaRequests = maxExtraIdeaRequests;

        if (string.IsNullOrEmpty(introduction))
        {
            project.Introduction = null;
        }
        else if (project.Introduction is null)
        {
            EntityValidator.ValidateProperty(new Introduction(), introduction, nameof(Introduction.Text));
            project.Introduction = new Introduction { Text = introduction };
        }
        else
        {
            EntityValidator.ValidateProperty(project.Introduction, introduction, nameof(Introduction.Text));
            project.Introduction.Text = introduction;
        }

        projectRepository.Update(project);
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

    public async Task<Project> DuplicateProjectAsync(int sourceProjectId, string newName, int platformId, string newShortForm = null, CancellationToken ct = default)
    {
        var src = await projectRepository.ReadByIdForDuplicationReadOnlyAsync(sourceProjectId);
        if (src is null)
            throw new KeyNotFoundException($"Project {sourceProjectId} not found.");

        if (src.ProjectPlatformId != platformId)
            throw new UnauthorizedAccessException("Source project belongs to a different platform.");

        var candidate = string.IsNullOrWhiteSpace(newShortForm)
            ? shortFormService.Normalize(newName)
            : newShortForm;

        if (!shortFormService.IsValid(candidate))
            throw new ValidationException($"'{candidate}' is not a valid shortform. Use lowercase letters, digits, and single hyphens only.");

        if (shortFormService.IsReserved(candidate))
            throw new ValidationException($"'{candidate}' is a reserved shortform and cannot be used.");

        var uniqueShortForm = await shortFormService.EnsureUniqueAsync(candidate);

        var copy = new Project
        {
            Name = newName,
            ShortForm = uniqueShortForm,
            Topic = src.Topic,
            ImageUrl = src.ImageUrl,
            FormType = src.FormType,
            AiAssistance = src.AiAssistance,
            MinAge = src.MinAge,
            MaxAge = src.MaxAge,
            RequestContactInfo = src.RequestContactInfo,
            IdeaPageSize = src.IdeaPageSize,
            ExtraIdeasReplaceExisting = src.ExtraIdeasReplaceExisting,
            MaxExtraIdeaRequests = src.MaxExtraIdeaRequests,
            CommentPageSize = src.CommentPageSize,
            ProjectPlatformId = platformId,
            ProjectState = ProjectState.Draft,
            CreatedAt = DateTime.UtcNow,
            ViewCount = 0,
        };

        EntityValidator.ValidateProperty(copy, newName, nameof(Project.Name));

        if (src.Introduction is not null)
            copy.Introduction = new Introduction { Text = src.Introduction.Text };

        copy.Questionnaire = new Questionnaire();

        if (src.Questionnaire is not null)
        {
            var sectionMap = new Dictionary<int, Section>();
            var questionMap = new Dictionary<int, Question>();

            foreach (var srcSection in src.Questionnaire.Sections)
            {
                var newSection = new Section { Name = srcSection.Name, Description = srcSection.Description };
                sectionMap[srcSection.SectionId] = newSection;
                copy.Questionnaire.Sections.Add(newSection);
            }

            foreach (var srcQ in src.Questionnaire.Questions)
            {
                var newQ = new Question
                {
                    Text = srcQ.Text,
                    QuestionFormat = srcQ.QuestionFormat,
                    IsMandatory = srcQ.IsMandatory,
                    AnswerChoices = srcQ.AnswerChoices.ToList(),
                    Position = srcQ.Position,
                    ParentTriggerValue = srcQ.ParentTriggerValue,
                    Section = srcQ.Section is not null ? sectionMap[srcQ.Section.SectionId] : null,
                };
                foreach (var ea in srcQ.EndingAnswers)
                    newQ.EndingAnswers.Add(new EndingAnswer { Value = ea.Value });
                questionMap[srcQ.QuestionId] = newQ;
                copy.Questionnaire.Questions.Add(newQ);
            }

            // Second pass: remap parent-child links using new entity instances
            foreach (var srcQ in src.Questionnaire.Questions)
            {
                if (srcQ.ParentQuestion is not null && questionMap.TryGetValue(srcQ.ParentQuestion.QuestionId, out var newParent))
                    questionMap[srcQ.QuestionId].ParentQuestion = newParent;
            }
        }

        foreach (var srcCat in src.Categories)
            copy.Categories.Add(new Category { Name = srcCat.Name, Description = srcCat.Description });

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
