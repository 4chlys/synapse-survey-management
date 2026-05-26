using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Interfaces;
using SynapseEnqueteManagement.DAL.Interfaces;

namespace SynapseEnqueteManagement.BL.Implementation;

public class EmojiManager(IEmojiRepository emojiRepository) : IEmojiManager
{
    public Task<IEnumerable<Emoji>> GetAllReadOnlyAsync()
        => emojiRepository.ReadAllReadOnlyAsync();

    public Task<IEnumerable<Emoji>> GetAllForUpdateAsync()
        => emojiRepository.ReadAllForUpdateAsync();

    public Task<Emoji> GetByIdForUpdateAsync(int emojiId)
        => emojiRepository.ReadByIdForUpdateAsync(emojiId);

    public Task<IEnumerable<Emoji>> GetByIdsForUpdateAsync(IEnumerable<int> ids, CancellationToken ct = default)
        => emojiRepository.ReadByIdsForUpdateAsync(ids, ct);
}
