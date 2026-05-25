using System.Text.Json.Serialization;

namespace SynapseEnqueteManagement.BL.AiModeration;

public sealed class ModerationConfig
{
    [JsonPropertyName("use_ai")] public bool UseAi { get; init; } = true;

    public string DefaultLanguage { get; init; } = "nl";

    [JsonPropertyName("moderation_prompt")]
    public string ModerationPrompt { get; init; } = string.Empty;

    [JsonPropertyName("comment_moderation_prompt")]
    public string CommentModerationPrompt { get; init; } = string.Empty;

    [JsonPropertyName("rewrite_prompt")]
    public string RewritePrompt { get; init; } = string.Empty;

    [JsonPropertyName("image_moderation_prompt")]
    public string ImageModerationPrompt { get; init; } = string.Empty;

    [JsonPropertyName("banned_words")]
    public IReadOnlyList<BannedWordEntry> BannedWords { get; init; } = [];

    [JsonPropertyName("regex_patterns")]
    public IReadOnlyList<RegexPatternEntry> RegexPatterns { get; init; } = [];
}

public sealed class BannedWordEntry
{
    [JsonPropertyName("word")]   public string Word   { get; init; } = string.Empty;
    [JsonPropertyName("reason")] public string Reason { get; init; } = string.Empty;
}

public sealed class RegexPatternEntry
{
    [JsonPropertyName("pattern")]   public string Pattern   { get; init; } = string.Empty;
    [JsonPropertyName("reason")]    public string Reason    { get; init; } = string.Empty;
    [JsonPropertyName("idea_only")] public bool   IdeaOnly  { get; init; } = false;
}

/// <summary>Tuning parameters for the fuzzy banned-word matcher in <see cref="RegexModerationService"/>.</summary>
public sealed class FuzzyConfig
{
    [JsonPropertyName("min_skeleton_length")]
    public int MinSkeletonLength { get; init; } = 3;

    [JsonPropertyName("levenshtein_thresholds")]
    public LevenshteinThresholdsConfig LevenshteinThresholds { get; init; } = new();
}

public sealed class LevenshteinThresholdsConfig
{
    [JsonPropertyName("max_length_for_0_edits")] public int MaxLengthFor0Edits { get; init; } = 4;
    [JsonPropertyName("max_length_for_1_edit")]  public int MaxLengthFor1Edit  { get; init; } = 6;
    [JsonPropertyName("max_length_for_2_edits")] public int MaxLengthFor2Edits { get; init; } = 9;
}
