using System.Text.RegularExpressions;

namespace SynapseEnqueteManagement.BL.AiModeration;

/// <summary>
/// Compiled regex patterns for the fast (exact) moderation pass.
///
/// Word-level slur patterns are generated automatically from
/// <see cref="BannedWordList"/> so there is no duplication between the regex
/// layer and the fuzzy layer.  Structural patterns (threats, self-harm, PII,
/// spam) are defined here directly because they involve multi-word phrases or
/// formats that fuzzy word matching cannot handle.
/// </summary>
internal static class RegexModerationPatterns
{
    private static readonly RegexOptions Opts =
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled;

    /// <summary>Each entry: (compiled pattern, human-readable rejection reason).</summary>
    public static readonly IReadOnlyList<(Regex Pattern, string Reason)> All = BuildAll();

    private static IReadOnlyList<(Regex, string)> BuildAll()
    {
        var list = new List<(Regex, string)>();
        
        foreach (var group in BannedWordList.All.GroupBy(e => e.Reason))
        {
            string alternation = string.Join("|", group.Select(e => Regex.Escape(e.Word)));
            list.Add((new Regex($@"\b({alternation})\b", Opts), group.Key));
        }

        list.Add((new Regex(@"\b(ik\s+ga\s+je\s+(vermoorden|doodmaken|slaan|neerslaan))\b", Opts),
            "Tekst bevat een bedreiging."));

        list.Add((new Regex(@"\b(kill\s+you|murder|gonna\s+hurt)\b", Opts),
            "Tekst bevat een bedreiging."));

        list.Add((new Regex(@"\b(zelfmoord\s+plegen|mezelf\s+(snijden|pijnigen|beschadigen))\b", Opts),
            "Tekst bevat verwijzingen naar zelfbeschadiging. Vraag hulp als je het moeilijk hebt."));

        list.Add((new Regex(@"\b(cut\s+myself|end\s+my\s+life|suicide\s+method)\b", Opts),
            "Tekst bevat verwijzingen naar zelfbeschadiging. Vraag hulp als je het moeilijk hebt."));
        
        list.Add((new Regex(@"\b(0[1-9]\d{7,8}|\+32\s?\d[\s\d]{7,10})\b", Opts),
            "Vermijd het delen van persoonlijke contactgegevens."));

        list.Add((new Regex(@"\b[A-Z0-9._%+\-]+@[A-Z0-9.\-]+\.[A-Z]{2,}\b", Opts),
            "Vermijd het delen van persoonlijke contactgegevens."));
        
        list.Add((new Regex(@"\b(koop\s+nu|bestel\s+nu|gratis\s+geld|bitcoin|crypto\s+invest)\b", Opts),
            "Tekst lijkt op spam of reclame."));

        list.Add((new Regex(@"https?://\S+", Opts),
            "Externe links zijn niet toegestaan in reacties."));

        return list.AsReadOnly();
    }
}