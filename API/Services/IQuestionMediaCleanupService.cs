namespace SynapseEnqueteManagement.API.Services;

public interface IQuestionMediaCleanupService
{
    Task DeleteIfOrphanedAsync(string oldMediaUrl, string newMediaUrl, bool isEmbed);
    Task DeleteOnRemovalAsync(string mediaUrl, bool isEmbed);
}
