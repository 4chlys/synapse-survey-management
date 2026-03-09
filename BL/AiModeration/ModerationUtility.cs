using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace SynapseEnqueteManagement.BL.AiModeration;

/// <summary>
/// Text-normalisation and fuzzy-matching helpers for the moderation pipeline.
///
/// Three complementary techniques are layered on top of the base regex scan:
///
/// 1. <b>Normalisation</b> – strips diacritics, converts leetspeak, collapses
///    punctuation noise so that "k@nk3r" and "kanker" become the same string.
///
/// 2. <b>Repeat collapsing</b> – "kaaaaanker" → "kanker" so that character-
///    flooding evasion is neutralised before the regex runs again.
///
/// 3. <b>Fuzzy matching</b> – each word in the input is compared against every
///    banned word using two independent signals:
///    <list type="bullet">
///      <item>
///        <b>Consonant-skeleton match</b> – vowels are stripped from both sides
///        ("knkr" matches "kanker") to catch vowel-omission evasion.
///      </item>
///      <item>
///        <b>Levenshtein edit distance</b> – catches single-character typos,
///        transpositions, and off-by-one substitutions.  The allowed distance
///        scales with word length so short words are held to a tighter standard.
///      </item>
///    </list>
///
/// All methods are pure / stateless so they can be called freely without DI.
/// </summary>
public static class ModerationUtility
{
    private static readonly Dictionary<char, char> LeetMap = new()
    {
        { '0', 'o' }, { '1', 'i' }, { '3', 'e' }, { '4', 'a' },
        { '5', 's' }, { '7', 't' }, { '8', 'b' }, { '@', 'a' },
        { '!', 'i' }, { '$', 's' },
    };
    
    private static readonly HashSet<char> Vowels = new() { 'a', 'e', 'i', 'o', 'u' };

    private static readonly Regex MultiSpaceRe =
        new(@"\s+", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex RepeatRe =
        new(@"(.)\1{2,}", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>
    /// Produces a canonical lowercase form of <paramref name="input"/> suitable
    /// for moderation pattern matching.
    ///
    /// Steps applied in order:
    /// <list type="number">
    ///   <item>Lowercase + Unicode NFD decomposition.</item>
    ///   <item>Non-spacing combining marks (diacritics) are dropped.</item>
    ///   <item>Leet-speak digits / symbols are replaced with their letter equivalents.</item>
    ///   <item>Non-alphanumeric, non-whitespace characters become spaces.</item>
    ///   <item>Runs of whitespace are collapsed to a single space.</item>
    /// </list>
    /// </summary>
    /// <param name="input">Raw user text.</param>
    /// <param name="stripWhitespace">
    /// When <c>true</c> all spaces are removed from the result, producing a
    /// single run-on token.  Useful for detecting split-word evasion like
    /// "k a n k e r".
    /// </param>
    public static string NormalizeForModeration(string input, bool stripWhitespace = false)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;

        string decomposed = input.ToLowerInvariant().Normalize(NormalizationForm.FormD);

        var sb = new StringBuilder(decomposed.Length);

        foreach (char c in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
                continue;

            char ch = LeetMap.GetValueOrDefault(c, c);

            if (char.IsLetterOrDigit(ch))
                sb.Append(ch);
            else if (char.IsWhiteSpace(ch))
                sb.Append(' ');
            else
                sb.Append(' ');
        }

        string result = sb.ToString();

        return stripWhitespace
            ? result.Replace(" ", "")
            : MultiSpaceRe.Replace(result, " ").Trim();
    }

    /// <summary>
    /// Collapses runs of three or more identical consecutive characters down to
    /// two, then further to one if the doubled form still doesn't match common
    /// Dutch/English spelling (heuristic: reduce to one for most consonants).
    ///
    /// Examples:
    /// <code>
    /// "kaaaaanker"  → "kanker"
    /// "fuuuuck"     → "fuck"
    /// "niggggger"   → "nigger"  (legitimate double-g kept)
    /// </code>
    ///
    /// The collapse is intentionally aggressive (3+ → 1) because legitimate
    /// Dutch/English words almost never rely on triple-or-more characters.
    /// </summary>
    public static string CollapseRepeats(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;

        string twoMax = RepeatRe.Replace(input, "$1$1");

        var sb = new StringBuilder(twoMax.Length);
        char prev = '\0';

        foreach (char c in twoMax)
        {
            if (c == prev && !Vowels.Contains(c))
            {
                continue;
            }
            sb.Append(c);
            prev = c;
        }

        return sb.ToString();
    }

    /// <summary>
    /// Returns the consonant skeleton of <paramref name="word"/> by removing
    /// all vowel characters.
    ///
    /// Examples:
    /// <code>
    /// "kanker"  → "knkr"
    /// "faggot"  → "fggt"
    /// "nigger"  → "nggr"
    /// </code>
    ///
    /// Used to detect vowel-omission evasion ("knkr", "fgt").
    /// Only meaningful on already-normalised, single-word strings.
    /// </summary>
    public static string ToConsonantSkeleton(string word)
    {
        if (string.IsNullOrEmpty(word)) return string.Empty;

        var sb = new StringBuilder(word.Length);
        foreach (char c in word)
        {
            if (!Vowels.Contains(c))
                sb.Append(c);
        }
        return sb.ToString();
    }

    /// <summary>
    /// Computes the Levenshtein edit distance between <paramref name="a"/> and
    /// <paramref name="b"/> using a space-optimised two-row DP table.
    ///
    /// Returns early once the running minimum exceeds <paramref name="maxDistance"/>
    /// to avoid unnecessary work when only a threshold check is needed.
    /// </summary>
    public static int LevenshteinDistance(string a, string b, int maxDistance = int.MaxValue)
    {
        if (a.Length == 0) return b.Length;
        if (b.Length == 0) return a.Length;

        if (a.Length < b.Length) (a, b) = (b, a);

        int[] prev = new int[b.Length + 1];
        int[] curr = new int[b.Length + 1];

        for (int j = 0; j <= b.Length; j++) prev[j] = j;

        for (int i = 1; i <= a.Length; i++)
        {
            curr[0] = i;
            int rowMin = curr[0];

            for (int j = 1; j <= b.Length; j++)
            {
                int cost = a[i - 1] == b[j - 1] ? 0 : 1;
                curr[j] = Math.Min(
                    Math.Min(curr[j - 1] + 1, prev[j] + 1),
                    prev[j - 1] + cost);
                rowMin = Math.Min(rowMin, curr[j]);
            }

            if (rowMin > maxDistance) return rowMin;

            (prev, curr) = (curr, prev);
        }

        return prev[b.Length];
    }

    /// <summary>
    /// Returns <c>true</c> when any whitespace-delimited token in
    /// <paramref name="normalizedText"/> is considered a fuzzy match for
    /// <paramref name="bannedWord"/> using the two signals below.
    ///
    /// <b>Signal A – consonant skeleton equality</b>:
    /// both the token and the banned word are reduced to their consonant
    /// skeletons and compared exactly.  This catches vowel-omission evasion
    /// such as "knkr" for "kanker", as long as the skeleton is at least
    /// <paramref name="minSkeletonLength"/> characters (prevents short
    /// coincidental matches like "tr" matching "tering").
    ///
    /// <b>Signal B – Levenshtein distance</b>:
    /// the allowed edit distance scales with the length of the banned word:
    /// <list type="table">
    ///   <listheader><term>Banned word length</term><description>Max edits</description></listheader>
    ///   <item><term>1–4 chars</term><description>0  (exact only)</description></item>
    ///   <item><term>5–6 chars</term><description>1  (one typo)</description></item>
    ///   <item><term>7–9 chars</term><description>2</description></item>
    ///   <item><term>10+ chars</term><description>3</description></item>
    /// </list>
    ///
    /// Only tokens whose length is within ±2 of the banned word length are
    /// considered for edit-distance comparison to avoid O(n²) false positives
    /// on very different-length strings.
    /// </summary>
    /// <param name="normalizedText">
    /// Already-normalised (and repeat-collapsed) input string, as returned by
    /// <see cref="NormalizeForModeration"/> followed by <see cref="CollapseRepeats"/>.
    /// </param>
    /// <param name="bannedWord">
    /// A single lowercase word from <see cref="BannedWordList"/>.
    /// </param>
    /// <param name="minSkeletonLength">
    /// Minimum consonant-skeleton length required before skeleton equality is
    /// accepted as a match.  Defaults to 3 to prevent very short skeletons
    /// (e.g. "tr") from producing false positives.
    /// </param>
    public static bool FuzzyContainsBannedWord(
        string normalizedText,
        string bannedWord,
        int minSkeletonLength = 3)
    {
        if (string.IsNullOrEmpty(normalizedText) || string.IsNullOrEmpty(bannedWord))
            return false;

        string bannedSkeleton = ToConsonantSkeleton(bannedWord);
        int    maxEdits       = AllowedEdits(bannedWord.Length);

        foreach (string token in normalizedText.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if (bannedSkeleton.Length >= minSkeletonLength)
            {
                string tokenSkeleton = ToConsonantSkeleton(token);
                if (tokenSkeleton == bannedSkeleton)
                    return true;
            }

            if (Math.Abs(token.Length - bannedWord.Length) <= 2)
            {
                int distance = LevenshteinDistance(token, bannedWord, maxEdits);
                if (distance <= maxEdits)
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Returns the maximum Levenshtein edit distance allowed for a banned word
    /// of the given length.  Shorter words require exact (or near-exact) matches
    /// to keep the false-positive rate acceptable.
    /// </summary>
    private static int AllowedEdits(int wordLength) => wordLength switch
    {
        <= 4 => 0, 
        <= 6 => 1,
        <= 9 => 2,
        _    => 3
    };
}