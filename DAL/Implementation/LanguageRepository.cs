using Microsoft.EntityFrameworkCore;
using SynapseEnqueteManagement.BL.Domain.I18n.Entities;
using SynapseEnqueteManagement.DAL.Interfaces;

namespace SynapseEnqueteManagement.DAL.Implementation;

public class LanguageRepository(DbContext context) : Repository<Language>(context), ILanguageRepository
{
    public Task<Language> ReadByCodeReadOnlyAsync(string code)
        => Context.Set<Language>().AsNoTracking().FirstOrDefaultAsync(l => l.Code == code);

    public Task<Language> ReadByCodeForUpdateAsync(string code)
        => Context.Set<Language>().FirstOrDefaultAsync(l => l.Code == code);

    public async Task<IEnumerable<Language>> ReadAllActiveReadOnlyAsync()
        => await Context.Set<Language>().AsNoTracking().Where(l => l.IsActive).ToListAsync();

    public async Task<IEnumerable<Language>> ReadByCodesForUpdateAsync(IEnumerable<string> codes)
    {
        var set = codes.ToHashSet();
        return await Context.Set<Language>().Where(l => set.Contains(l.Code)).ToListAsync();
    }
}
