namespace SynapseEnqueteManagement.BL.Helpers;

/// <summary>
/// Centralises the ", "-delimited encoding used to persist MultipleChoice answer values.
/// </summary>
public static class MultipleChoiceValueSerializer
{
    private const string Delimiter = ", ";

    /// <summary>
    /// Splits a stored MultipleChoice value into its individual answer strings.
    /// RemoveEmptyEntries guards against leading/trailing delimiters that would
    /// otherwise produce phantom empty strings when SplitSelected evaluates branching.
    /// </summary>
    public static IReadOnlyList<string> Split(string rawValue)
        => rawValue.Split(Delimiter, StringSplitOptions.RemoveEmptyEntries);

    /// <summary>Joins individual answer strings into the stored MultipleChoice format.</summary>
    public static string Join(IEnumerable<string> values)
        => string.Join(Delimiter, values);
}
