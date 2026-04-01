namespace SynapseEnqueteManagement.BL.AiIdeaSupport;

/// <summary>
/// Conversational AI agent that helps participants articulate and substantiate
/// their ideas before submission.
///
/// The service is intentionally stateless: the full conversation history is
/// supplied with every call (same pattern as the moderation service).
/// </summary>
public interface IAiIdeaSupportService
{
    /// <summary>
    /// Produces the next assistant turn given the current conversation state.
    /// Never throws — returns a safe fallback message on error.
    /// </summary>
    Task<IdeaSupportChatReply> ChatAsync(
        IdeaSupportChatRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Standalone endpoint: polishes a draft idea without any conversation history.
    /// Returns null when the text cannot meaningfully be improved.
    /// Never throws.
    /// </summary>
    Task<IdeaSupportRefineResult> RefineAsync(
        string draftText,
        CancellationToken ct = default);
}