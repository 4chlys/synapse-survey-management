using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;
using SynapseEnqueteManagement.DAL.Interfaces;
using SynapseEnqueteManagement.BL.Interfaces;

namespace SynapseEnqueteManagement.BL.Implementation;

public class ProjectManager(
    IProjectRepository projectRepository,
    IEmojiRepository emojiRepository,
    IUnitOfWork uow) : IProjectManager
{
    public Task<Project> GetProjectByIdAsync(int projectId)
        => projectRepository.ReadByIdWithIntroductionReadOnlyAsync(projectId);

    public async Task<Project> CreateProjectAsync(string name, string topic, string introduction, string imageUrl, FormType formType, AiAssistanceType aiAssistance, int? minAge, int? maxAge, bool requestContactInfo)
    {
        var project = new Project
        {
            Name = name,
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
        await uow.SaveChangesAsync();
        return created;
    }

    public async Task<Project> UpdateProjectAsync(int projectId, string name, string topic, string introduction, string imageUrl, FormType formType, AiAssistanceType aiAssistance, int? minAge, int? maxAge, bool requestContactInfo)
    {
        var project = await projectRepository.ReadByIdWithIntroductionForUpdateAsync(projectId);
        if (project is null)
            throw new ArgumentException($"Project {projectId} not found.");

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
        await uow.SaveChangesAsync();
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
