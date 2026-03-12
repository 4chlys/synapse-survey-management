using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace SynapseEnqueteManagement.BL.AiModeration;

/// <summary>
/// Text-normalisation and fuzzy-matching helpers for the moderation pipeline.
/// Pure / stateless methods; call freely without DI.
///
/// The fuzzy-matching thresholds (min skeleton length, Levenshtein buckets) that
/// were previously hardcoded are now read from <see cref="AiConfig"/> and passed
/// in by the caller (<see cref="GoogleVertexAiService"/>).
/// </summary>
public static class ModerationUtility
{
    private static readonly Dictionary<char, char> LeetMap = new()
    {
        { '0', 'o' }, { '1', 'i' }, { '3', 'e' }, { '4', 'a' },
        { '5', 's' }, { '7', 't' }, { '8', 'b' }, { '@', 'a' },
        { '!', 'i' }, { '$', 's' },
    };

    private static readonly HashSet<char> Vowels = ['a', 'e', 'i', 'o', 'u'];

    private static readonly Regex MultiSpaceRe =
        new(@"\s+", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex RepeatRe =
        new(@"(.)\1{2,}", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    
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

            if (char.IsLetterOrDigit(ch)) sb.Append(ch);
            else                          sb.Append(' ');
        }

        string result = sb.ToString();
        return stripWhitespace
            ? result.Replace(" ", "")
            : MultiSpaceRe.Replace(result, " ").Trim();
    }

    public static string CollapseRepeats(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;

        string twoMax = RepeatRe.Replace(input, "$1$1");
        var sb   = new StringBuilder(twoMax.Length);
        char prev = '\0';

        foreach (char c in twoMax)
        {
            if (c == prev && !Vowels.Contains(c)) continue;
            sb.Append(c);
            prev = c;
        }

        return sb.ToString();
    }

    public static string ToConsonantSkeleton(string word)
    {
        if (string.IsNullOrEmpty(word)) return string.Empty;

        var sb = new StringBuilder(word.Length);
        foreach (char c in word)
            if (!Vowels.Contains(c)) sb.Append(c);
        return sb.ToString();
    }

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
                curr[j] = Math.Min(Math.Min(curr[j - 1] + 1, prev[j] + 1), prev[j - 1] + cost);
                rowMin  = Math.Min(rowMin, curr[j]);
            }

            if (rowMin > maxDistance) return rowMin;
            (prev, curr) = (curr, prev);
        }

        return prev[b.Length];
    }

    /// <summary>
    /// Returns true when any whitespace-delimited token in <paramref name="normalizedText"/>
    /// is a fuzzy match for <paramref name="bannedWord"/>.
    ///
    /// Thresholds are supplied by the caller so they can be driven from <see cref="AiConfig"/>
    /// without making this utility class depend on DI.
    /// </summary>
    public static bool FuzzyContainsBannedWord(
        string normalizedText,
        string bannedWord,
        int    minSkeletonLength,
        int    maxLengthFor0Edits,
        int    maxLengthFor1Edit,
        int    maxLengthFor2Edits)
    {
        if (string.IsNullOrEmpty(normalizedText) || string.IsNullOrEmpty(bannedWord))
            return false;

        string bannedSkeleton = ToConsonantSkeleton(bannedWord);
        int    maxEdits       = AllowedEdits(bannedWord.Length, maxLengthFor0Edits, maxLengthFor1Edit, maxLengthFor2Edits);

        foreach (string token in normalizedText.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if (bannedSkeleton.Length >= minSkeletonLength)
            {
                if (ToConsonantSkeleton(token) == bannedSkeleton)
                    return true;
            }

            if (Math.Abs(token.Length - bannedWord.Length) <= 2)
            {
                if (LevenshteinDistance(token, bannedWord, maxEdits) <= maxEdits)
                    return true;
            }
        }

        return false;
    }

    private static int AllowedEdits(int wordLength, int t0, int t1, int t2) =>
        wordLength <= t0 ? 0 :
        wordLength <= t1 ? 1 :
        wordLength <= t2 ? 2 : 3;
}