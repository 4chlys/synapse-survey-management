using SynapseEnqueteManagement.BL.Domain.I18n.Entities;

namespace SynapseEnqueteManagement.BL.Interfaces;

// Single gateway for resolving language codes against the DB. Owns the only read against
// ILanguageRepository that exists across the codebase: "given some codes, return the entities,
// throw if missing/inactive". All managers that previously injected ILanguageRepository to do
// this validation now depend on this resolver instead.
public interface ILanguageResolver
{
    // Returns a {code → Language} dictionary covering every input code. Throws
    // LocalizedValidationException("Validation.Language.UnknownCodes", <comma-list>) when codes
    // are not in the DB; when requireActive, also throws
    // LocalizedValidationException("Validation.Project.InactiveLanguageNotAllowed") if any
    // resolved language is inactive. Empty/null input returns an empty dictionary.
    Task<IReadOnlyDictionary<string, Language>> ResolveAsync(
        IEnumerable<string> codes, bool requireActive = true);

    // Resolves a single code. Throws KeyNotFoundException with the participant-flow message
    // when the code is missing or (when requireActive) inactive.
    Task<Language> ResolveSingleAsync(string code, bool requireActive = true);
}
