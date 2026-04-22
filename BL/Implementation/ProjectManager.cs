using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;
using SynapseEnqueteManagement.DAL;
using SynapseEnqueteManagement.BL.Interfaces;

namespace SynapseEnqueteManagement.BL.Implementation;

public class ProjectManager(IRepository repository, IUnitOfWork uow) : IProjectManager
{
    public async Task<Project> GetProjectByIdAsync(int projectId)
    {
        return await repository.ReadByIdAsync<Project>(projectId,
            includes: [p => p.Introduction]);
    }

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

        var created = await repository.CreateAsync(project);
        await uow.SaveChangesAsync();
        return created;
    }

    public async Task<Project> UpdateProjectAsync(int projectId, string name, string topic, string introduction, string imageUrl, FormType formType, AiAssistanceType aiAssistance, int? minAge, int? maxAge, bool requestContactInfo)
    {
        var project = await repository.ReadByIdAsync<Project>(projectId,
            includes: [p => p.Introduction]);
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

        repository.Update(project);
        await uow.SaveChangesAsync();
        return project;
    }

    public async Task<IEnumerable<Project>> GetAllProjectsAsync()
    {
        return await repository.ReadAsync<Project>();
    }

    public async Task<IEnumerable<Emoji>> GetAllEmojisAsync()
    {
        return await repository.ReadAsync<Emoji>();
    }

    public async Task<bool> EnableEmojiForProjectAsync(int projectId, int emojiId, CancellationToken ct = default)
    {
        var project = await repository.ReadByIdAsync<Project>(projectId, includes: [p => p.Emojis], tracked: true);
        if (project == null) return false;

        var emoji = await repository.ReadByIdAsync<Emoji>(emojiId, tracked: true);
        if (emoji == null) return false;

        if (project.Emojis.Any(e => e.EmojiId == emojiId)) return true;

        project.Emojis.Add(emoji);
        repository.Update(project);
        await uow.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> DisableEmojiForProjectAsync(int projectId, int emojiId, CancellationToken ct = default)
    {
        var project = await repository.ReadByIdAsync<Project>(projectId, includes: [p => p.Emojis], tracked: true);
        if (project == null) return false;

        var emoji = project.Emojis.FirstOrDefault(e => e.EmojiId == emojiId);
        if (emoji == null) return false;

        project.Emojis.Remove(emoji);
        repository.Update(project);
        await uow.SaveChangesAsync(ct);
        return true;
    }
}
