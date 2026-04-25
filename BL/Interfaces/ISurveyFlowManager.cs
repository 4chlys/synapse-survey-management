using SynapseEnqueteManagement.BL.Domain.Project.Entities;

namespace SynapseEnqueteManagement.BL.Interfaces;

public interface ISurveyFlowManager
{
    Task<Question> GetNextQuestionAsync(int currentQuestionId, int participantId);
    Task<Question> GetPreviousQuestionAsync(int currentQuestionId, int participantId);
    Task<Question> GetResumeQuestionAsync(int projectId, int participantId);
    Task<IEnumerable<int>> ComputeVisiblePathAsync(int projectId, int participantId);
}
