using SynapseEnqueteManagement.BL.Domain.Project.Entities;

namespace SynapseEnqueteManagement.DAL.Interfaces;

public interface IEmojiRepository : IRepository<Emoji>
{
    Task<IEnumerable<Emoji>> ReadByIdsForUpdateAsync(IEnumerable<int> ids, CancellationToken ct = default);
}
