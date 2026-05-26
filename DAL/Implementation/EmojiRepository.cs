using Microsoft.EntityFrameworkCore;
using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.DAL.Interfaces;

namespace SynapseEnqueteManagement.DAL.Implementation;

public class EmojiRepository(DbContext context) : Repository<Emoji>(context), IEmojiRepository
{
    public async Task<IEnumerable<Emoji>> ReadByIdsForUpdateAsync(IEnumerable<int> ids, CancellationToken ct = default)
        => await Context.Set<Emoji>().Where(e => ids.Contains(e.EmojiId)).ToListAsync(ct);
}
