namespace SynapseEnqueteManagement.BL.AiSocialMedia;

public sealed record SocialPostStats(
    int ViewCount,
    int CompletionCount,
    double CompletionRate,
    IReadOnlyList<string> TopIdeaDescriptions,
    IReadOnlyList<string> TopCategoryNames);

public sealed record SocialPostRequest(
    SocialPlatform Platform,
    string ProjectName,
    string ProjectTopic,
    string ProjectIntroduction,
    string OrganisationName,
    string UserDescription,
    SocialPostStats Stats);

public sealed record SocialPostReply(
    string Copy,
    IReadOnlyList<string> SuggestedHashtags,
    int CharacterCount);
