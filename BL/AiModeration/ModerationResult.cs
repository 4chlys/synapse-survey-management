namespace SynapseEnqueteManagement.BL.AiModeration;

public class ModerationResult
{
    /// <summary>Whether the content passed moderation (i.e. is acceptable).</summary>
    public bool IsAcceptable { get; init; }

    /// <summary>Human-readable reason when content is rejected.</summary>
    public string Reason { get; init; }

    /// <summary>Which layer caught the issue: "Regex", "AI", or null when acceptable.</summary>
    public string FlaggedBy { get; init; }

    public static ModerationResult Acceptable() =>
        new() { IsAcceptable = true };

    public static ModerationResult RejectedByRegex(string reason) =>
        new() { IsAcceptable = false, Reason = reason, FlaggedBy = "Regex" };

    public static ModerationResult RejectedByAi(string reason) =>
        new() { IsAcceptable = false, Reason = reason, FlaggedBy = "AI" };

    public static ModerationResult ServiceUnavailable() =>
        new() { IsAcceptable = false, Reason = "AI moderation service unavailable", FlaggedBy = "Service" };
}