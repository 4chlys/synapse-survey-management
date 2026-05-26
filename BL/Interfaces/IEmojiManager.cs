using SynapseEnqueteManagement.BL.Domain.Project.Entities;

namespace SynapseEnqueteManagement.BL.Interfaces;

public interface IEmojiManager
{
    Task<IEnumerable<Emoji>> GetAllReadOnlyAsync();
    Task<Emoji> GetByIdForUpdateAsync(int emojiId);
}
