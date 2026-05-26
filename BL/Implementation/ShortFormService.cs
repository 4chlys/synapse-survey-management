using System.Text.RegularExpressions;
using SynapseEnqueteManagement.BL.Exceptions;
using SynapseEnqueteManagement.BL.Interfaces;
using SynapseEnqueteManagement.DAL.Interfaces;

namespace SynapseEnqueteManagement.BL.Implementation;

public class ShortFormService(IProjectRepository projectRepository, IEnumerable<string> reservedSubdomains) : IShortFormService
{
    private static readonly HashSet<string> HardReserved =
        new(StringComparer.OrdinalIgnoreCase) { "admin", "api", "www" };

    private static readonly Regex ValidPattern =
        new(@"^[a-z0-9](-?[a-z0-9])*$", RegexOptions.Compiled);

    private readonly HashSet<string> _reservedSet = BuildReservedSet(reservedSubdomains);

    private static HashSet<string> BuildReservedSet(IEnumerable<string> configured)
    {
        var set = new HashSet<string>(configured, StringComparer.OrdinalIgnoreCase);
        foreach (var r in HardReserved)
            set.Add(r);
        return set;
    }

    public string Normalize(string source)
    {
        if (string.IsNullOrWhiteSpace(source))
            return string.Empty;

        var lower = source.ToLowerInvariant();
        var replaced = Regex.Replace(lower, @"[^a-z0-9]+", "-");
        var trimmed = replaced.Trim('-');
        if (trimmed.Length > 30)
            trimmed = trimmed[..30].TrimEnd('-');
        return trimmed;
    }

    public async Task<string> EnsureUniqueAsync(string candidate, int? excludeProjectId = null)
    {
        if (!await projectRepository.ExistsByShortFormAsync(candidate, excludeProjectId))
            return candidate;

        for (int suffix = 2; ; suffix++)
        {
            var suffixStr = $"-{suffix}";
            var baseLen = Math.Min(candidate.Length, 30 - suffixStr.Length);
            var numbered = candidate[..baseLen].TrimEnd('-') + suffixStr;
            if (!await projectRepository.ExistsByShortFormAsync(numbered, excludeProjectId))
                return numbered;
        }
    }

    public bool IsReserved(string candidate)
        => _reservedSet.Contains(candidate);

    public bool IsValid(string candidate)
        => !string.IsNullOrEmpty(candidate)
           && candidate.Length <= 30
           && ValidPattern.IsMatch(candidate);

    public Task<string> ResolveAsync(string desired, string defaultName, int? excludeProjectId = null)
    {
        var candidate = string.IsNullOrWhiteSpace(desired) ? Normalize(defaultName) : desired;
        if (!IsValid(candidate))
            throw new LocalizedValidationException("Validation.ShortForm.Invalid", candidate);
        if (IsReserved(candidate))
            throw new LocalizedValidationException("Validation.ShortForm.Reserved", candidate);

        return EnsureUniqueAsync(candidate, excludeProjectId);
    }
}
