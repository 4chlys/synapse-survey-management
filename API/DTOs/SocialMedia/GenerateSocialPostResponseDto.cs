namespace SynapseEnqueteManagement.API.DTOs.SocialMedia;

public class GenerateSocialPostResponseDto
{
    public string Copy { get; set; } = "";
    public List<string> SuggestedHashtags { get; set; } = [];
    public int CharacterCount { get; set; }
    public string Platform { get; set; } = "";
}
