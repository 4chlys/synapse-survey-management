using SynapseEnqueteManagement.BL.AiSupport;

namespace SynapseEnqueteManagement.BL.AiIdeaSupport;

public interface IAiIdeaSupportService : IAiSupportService<IdeaSupportChatRequest, IdeaSupportChatReply>
{
    /// <summary>
    /// Standalone polish of a draft without conversation history.
    /// Returns null when the text cannot meaningfully be improved.
    /// </summary>
    Task<IdeaSupportRefineResult> RefineAsync(
        string draftText,
        string language,
        int projectId,
        CancellationToken ct = default);
}
