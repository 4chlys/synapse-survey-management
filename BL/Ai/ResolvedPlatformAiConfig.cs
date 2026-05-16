using Microsoft.Extensions.AI;
using SynapseEnqueteManagement.BL.AiCommentSupport;
using SynapseEnqueteManagement.BL.AiIdeaSupport;
using SynapseEnqueteManagement.BL.AiModeration;
using SynapseEnqueteManagement.BL.AiQuestionSupport;

namespace SynapseEnqueteManagement.BL.Ai;

public sealed record ResolvedPlatformAiConfig(
    IChatClient             ChatClient,
    string                  SelectedModelId,
    ModerationConfig        Moderation,
    AiIdeaSupportConfig     IdeaSupport,
    AiCommentSupportConfig  CommentSupport,
    AiQuestionSupportConfig QuestionSupport
);