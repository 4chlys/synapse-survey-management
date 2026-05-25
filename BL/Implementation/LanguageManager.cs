using SynapseEnqueteManagement.BL.Domain.I18n.Entities;
using SynapseEnqueteManagement.BL.Interfaces;
using SynapseEnqueteManagement.DAL.Interfaces;

namespace SynapseEnqueteManagement.BL.Implementation;

public class LanguageManager(ILanguageRepository languageRepository) : ILanguageManager
{
    public Task<Language> GetByCodeAsync(string code)
        => languageRepository.ReadByCodeReadOnlyAsync(code);

    public Task<IEnumerable<Language>> GetAllActiveAsync()
        => languageRepository.ReadAllActiveReadOnlyAsync();

    public Task<IEnumerable<Language>> GetByCodesAsync(IEnumerable<string> codes)
        => languageRepository.ReadByCodesForUpdateAsync(codes);
}
