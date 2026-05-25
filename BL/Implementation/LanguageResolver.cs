using SynapseEnqueteManagement.BL.Domain.I18n.Entities;
using SynapseEnqueteManagement.BL.Exceptions;
using SynapseEnqueteManagement.BL.Interfaces;
using SynapseEnqueteManagement.DAL.Interfaces;

namespace SynapseEnqueteManagement.BL.Implementation;

public class LanguageResolver(ILanguageRepository languageRepository) : ILanguageResolver
{
    public async Task<IReadOnlyDictionary<string, Language>> ResolveAsync(
        IEnumerable<string> codes, bool requireActive = true)
    {
        var distinct = codes?.Distinct().ToList() ?? new List<string>();
        if (distinct.Count == 0)
            return new Dictionary<string, Language>();

        var resolved = (await languageRepository.ReadByCodesForUpdateAsync(distinct)).ToList();
        if (requireActive && resolved.Any(l => !l.IsActive))
            throw new LocalizedValidationException("Validation.Project.InactiveLanguageNotAllowed");

        var byCode = resolved.ToDictionary(l => l.Code);
        var missing = distinct.Where(c => !byCode.ContainsKey(c)).ToList();
        if (missing.Count > 0)
            throw new LocalizedValidationException(
                "Validation.Language.UnknownCodes", string.Join(", ", missing));

        return byCode;
    }

    public async Task<Language> ResolveSingleAsync(string code, bool requireActive = true)
    {
        var language = await languageRepository.ReadByCodeForUpdateAsync(code);
        if (language is null || (requireActive && !language.IsActive))
            throw new KeyNotFoundException($"Language '{code}' is not a supported active language.");
        return language;
    }
}
