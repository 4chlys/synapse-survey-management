using SynapseEnqueteManagement.BL.Domain.Project.Entities;
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