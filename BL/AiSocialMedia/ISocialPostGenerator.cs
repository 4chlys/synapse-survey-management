namespace SynapseEnqueteManagement.BL.AiSocialMedia;

public interface ISocialPostGenerator
{
    Task<SocialPostReply> GenerateAsync(SocialPostRequest request, CancellationToken ct = default);
}
