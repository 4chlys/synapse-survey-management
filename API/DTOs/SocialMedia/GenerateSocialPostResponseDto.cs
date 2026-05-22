namespace SynapseEnqueteManagement.API.DTOs.SocialMedia;

public record GenerateSocialPostResponseDto
{
    public string Copy { get; init; } = "";
    public ICollection<string> SuggestedHashtags { get; init; } = [];
    public int CharacterCount { get; init; }
    public string Platform { get; init; } = "";
}
