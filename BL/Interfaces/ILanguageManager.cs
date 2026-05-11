using SynapseEnqueteManagement.BL.Domain.I18n.Entities;

namespace SynapseEnqueteManagement.BL.Interfaces;

public interface ILanguageManager
{
    Task<Language> GetByCodeAsync(string code);
    Task<IEnumerable<Language>> GetAllActiveAsync();
    Task<IEnumerable<Language>> GetByCodesAsync(IEnumerable<string> codes);
}
