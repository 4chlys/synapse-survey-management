namespace SynapseEnqueteManagement.API.Services;

public class QuestionMediaCleanupService(IStorageService storageService, ILogger<QuestionMediaCleanupService> logger)
    : IQuestionMediaCleanupService
{
    public async Task DeleteIfOrphanedAsync(string oldMediaUrl, string newMediaUrl, bool isEmbed)
    {
        if (string.IsNullOrEmpty(oldMediaUrl) || oldMediaUrl == newMediaUrl || isEmbed)
            return;

        try { await storageService.DeleteAsync(oldMediaUrl); }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[Media] Failed to delete orphaned question media {Url}", oldMediaUrl);
        }
    }

    public async Task DeleteOnRemovalAsync(string mediaUrl, bool isEmbed)
    {
        if (string.IsNullOrEmpty(mediaUrl) || isEmbed)
            return;

        try { await storageService.DeleteAsync(mediaUrl); }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[Media] Failed to delete question media on question delete {Url}", mediaUrl);
        }
    }
}
