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

    public async Task<IEnumerable<Emoji>> GetEmojisForProjectAsync(int projectId)
    {
        var project = await projectRepository.ReadByIdWithEmojisReadOnlyAsync(projectId);
        return project?.Emojis ?? Enumerable.Empty<Emoji>();
    }

    public Task<Project> GetProjectByShortFormAsync(string shortForm)
        => projectRepository.ReadByShortFormReadOnlyAsync(shortForm);

    public async Task<Project> CreateProjectAsync(string name, string topic, string introduction, string imageUrl, FormType formType, AiAssistanceType aiAssistance, int? minAge, int? maxAge, bool requestContactInfo, string shortForm = null, CancellationToken ct = default)
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
            OrganisationName = name.Length > 50 ? name[..50] : name,
            Topic = topic,
            ImageUrl = imageUrl,
            FormType = formType,
            AiAssistance = aiAssistance,
            MinAge = minAge,
            MaxAge = maxAge,
            RequestContactInfo = requestContactInfo,
            ProjectState = ProjectState.Draft,
            Questionnaire = new Questionnaire(),
            CreatedAt = DateTime.UtcNow
        };
        if (!string.IsNullOrEmpty(introduction))
            project.Introduction = new Introduction { Text = introduction };

        var created = await projectRepository.CreateAsync(project);
        await uow.SaveChangesAsync(ct);
        return created;
    }

    public async Task<Project> UpdateProjectAsync(int projectId, string name, string topic, string introduction, string imageUrl, FormType formType, AiAssistanceType aiAssistance, int? minAge, int? maxAge, bool requestContactInfo, string shortForm = null, CancellationToken ct = default)
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

        project.Name = name;
        project.Topic = topic;
        project.ImageUrl = imageUrl;
        project.FormType = formType;
        project.AiAssistance = aiAssistance;
        project.MinAge = minAge;
        project.MaxAge = maxAge;
        project.RequestContactInfo = requestContactInfo;

        if (project.Introduction is null)
        {
            if (!string.IsNullOrEmpty(introduction))
                project.Introduction = new Introduction { Text = introduction };
        }
        else
        {
            project.Introduction.Text = introduction;
        }

        projectRepository.Update(project);
        await uow.SaveChangesAsync(ct);
        return project;
    }

    public Task<IEnumerable<Project>> GetAllProjectsAsync()
        => projectRepository.ReadAllReadOnlyAsync();

    public Task<IEnumerable<Emoji>> GetAllEmojisAsync()
        => emojiRepository.ReadAllReadOnlyAsync();

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
