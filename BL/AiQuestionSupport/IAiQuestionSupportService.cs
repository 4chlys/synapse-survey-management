using SynapseEnqueteManagement.BL.AiSupport;

namespace SynapseEnqueteManagement.BL.AiQuestionSupport;

public interface IAiQuestionSupportService
    : IAiSupportService<QuestionSupportChatRequest, QuestionSupportChatReply> { }
