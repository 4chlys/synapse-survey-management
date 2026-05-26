using SynapseEnqueteManagement.BL.Domain.Project.Entities;

namespace SynapseEnqueteManagement.BL.Interfaces;

public interface IEmojiManager
{
    Task<IEnumerable<Emoji>> GetAllReadOnlyAsync();
    Task<IEnumerable<Emoji>> GetAllForUpdateAsync();
    Task<Emoji> GetByIdForUpdateAsync(int emojiId);
    Task<IEnumerable<Emoji>> GetByIdsForUpdateAsync(IEnumerable<int> ids, CancellationToken ct = default);
}
