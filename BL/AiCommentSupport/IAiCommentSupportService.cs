namespace SynapseEnqueteManagement.BL.AiCommentSupport;

/// <summary>
/// Stateless AI agent that helps participants formulate a quick reaction to an idea.
/// The entire conversation history is supplied with every call.
/// Max two AI turns: intro (summarise idea) → feedback (after user sends comment draft).
/// </summary>
public interface IAiCommentSupportService
{
    /// <summary>
    /// Produces the next assistant turn. Never throws — returns a fallback reply on failure.
    /// </summary>
    Task<CommentSupportReply> ChatAsync(CommentSupportRequest request, CancellationToken ct = default);
}
