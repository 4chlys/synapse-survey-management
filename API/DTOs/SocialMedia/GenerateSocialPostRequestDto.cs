namespace SynapseEnqueteManagement.API.DTOs.SocialMedia;

public record GenerateSocialPostRequestDto
{
    public string Platform { get; init; } = "";
    public string UserDescription { get; init; } = "";
}
