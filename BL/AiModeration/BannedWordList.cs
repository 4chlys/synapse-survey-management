namespace SynapseEnqueteManagement.BL.AiModeration;

/// <summary>
/// Single source of truth for banned words that are checked by both the
/// exact-regex layer (<see cref="RegexModerationPatterns"/>) and the fuzzy
/// layer inside <see cref="GoogleVertexAiService.CheckWithRegex"/>.
///
/// Each entry pairs a canonical lowercase word with the Dutch rejection reason
/// shown to the user.  Multi-word phrases (threats, self-harm) are intentionally
/// kept in <see cref="RegexModerationPatterns"/> only because fuzzy word-level
/// matching does not apply to them.
///
/// <b>How to add a new word:</b>
/// Add it here.  The regex pattern (<see cref="RegexModerationPatterns"/>) and
/// the fuzzy scan both read from this list automatically.
/// </summary>
internal static class BannedWordList
{
    /// <summary>
    /// (canonicalWord, rejectionReason) pairs.
    /// Words must be lowercase, single tokens, already ASCII-normalised
    /// (no diacritics, no leet).
    /// </summary>
    public static readonly IReadOnlyList<(string Word, string Reason)> All =
        new List<(string, string)>
    {
        ("kanker",      "Tekst bevat grof taalgebruik."),
        ("kankerd",     "Tekst bevat grof taalgebruik."),
        ("tering",      "Tekst bevat grof taalgebruik."),
        ("tyfus",       "Tekst bevat grof taalgebruik."),
        ("godver",      "Tekst bevat grof taalgebruik."),
        ("godverdomme", "Tekst bevat grof taalgebruik."),
        ("klootzak",    "Tekst bevat grof taalgebruik."),
        ("kutwijf",     "Tekst bevat grof taalgebruik."),
        ("hoerenzoon",  "Tekst bevat grof taalgebruik."),
        
        ("nigger",   "Tekst bevat discriminerende taal."),
        ("nigga",    "Tekst bevat discriminerende taal."),
        ("faggot",   "Tekst bevat discriminerende taal."),
        ("retard",   "Tekst bevat discriminerende taal."),
        ("spastic",  "Tekst bevat discriminerende taal."),
        ("fuck",     "Tekst bevat grof taalgebruik."),
        ("shit",     "Tekst bevat grof taalgebruik."),
        ("bitch",    "Tekst bevat grof taalgebruik."),
        ("asshole",  "Tekst bevat grof taalgebruik."),
        ("bastard",  "Tekst bevat grof taalgebruik."),
    }.AsReadOnly();
}