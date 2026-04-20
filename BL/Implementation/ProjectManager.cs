using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;
using SynapseEnqueteManagement.DAL;
using SynapseEnqueteManagement.BL.Interfaces;

namespace SynapseEnqueteManagement.BL.Implementation;

public class ProjectManager(IRepository repository) : IProjectManager
{
    public async Task<Project> GetProjectByIdAsync(int projectId)
    {
        return await repository.ReadByIdAsync<Project>(projectId,
            includes: [p => p.Introduction]);
    }

    public async Task<Project> CreateProjectAsync(string name, string imageUrl)
    {
        var project = new Project
        {
            OrganisationName = name,
            OrganisationLogoUrl = imageUrl,
            ProjectState = ProjectState.Draft,
            FormType = FormType.VerticalScroll,
            Questionnaire = new Questionnaire(),
            CreatedAt = DateTime.UtcNow
        };
        return await repository.CreateAsync(project);
    }

    public async Task<IEnumerable<Project>> GetAllProjectsAsync()
    {
        return await repository.ReadAsync<Project>();
    }

    public async Task<IEnumerable<Emoji>> GetAllEmojisAsync()
    {
        return await repository.ReadAsync<Emoji>();
    }

    public async Task<bool> EnableEmojiForProjectAsync(int projectId, int emojiId)
    {
        var project = await repository.ReadByIdAsync<Project>(projectId, includes: [p => p.Emojis], tracked: true);
        if (project == null) return false;

        var emoji = await repository.ReadByIdAsync<Emoji>(emojiId, tracked: true);
        if (emoji == null) return false;

        if (project.Emojis.Any(e => e.EmojiId == emojiId)) return true;

        project.Emojis.Add(emoji);
        await repository.UpdateAsync(project);
        return true;
    }

    public async Task<bool> DisableEmojiForProjectAsync(int projectId, int emojiId)
    {
        var project = await repository.ReadByIdAsync<Project>(projectId, includes: [p => p.Emojis], tracked: true);
        if (project == null) return false;

        var emoji = project.Emojis.FirstOrDefault(e => e.EmojiId == emojiId);
        if (emoji == null) return false;

        project.Emojis.Remove(emoji);
        await repository.UpdateAsync(project);
        return true;
    }
}
