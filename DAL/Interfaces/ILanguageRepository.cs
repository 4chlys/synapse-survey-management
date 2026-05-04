using SynapseEnqueteManagement.BL.Domain.I18n.Entities;

namespace SynapseEnqueteManagement.DAL.Interfaces;

public interface ILanguageRepository : IRepository<Language>
{
    Task<Language> ReadByCodeReadOnlyAsync(string code);
    Task<Language> ReadByCodeForUpdateAsync(string code);
    Task<IEnumerable<Language>> ReadAllActiveReadOnlyAsync();
    Task<IEnumerable<Language>> ReadByCodesForUpdateAsync(IEnumerable<string> codes);
}
