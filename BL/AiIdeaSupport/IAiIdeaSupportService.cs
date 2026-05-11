using SynapseEnqueteManagement.BL.AiSupport;

namespace SynapseEnqueteManagement.BL.AiIdeaSupport;

public interface IAiIdeaSupportService : IAiSupportService<IdeaSupportChatRequest, IdeaSupportChatReply>
{
    Task<IdeaSupportRefineResult> RefineAsync(string draftText, CancellationToken ct = default);
}
