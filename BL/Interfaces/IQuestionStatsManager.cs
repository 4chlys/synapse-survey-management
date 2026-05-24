using SynapseEnqueteManagement.BL.Domain.Project.Enums;

namespace SynapseEnqueteManagement.BL.Interfaces;

public interface IQuestionStatsManager
{
    Task<SingleChoiceStats> ReadSingleChoiceStatsAsync(int projectId, int questionId);
    Task<MultipleChoiceStats> ReadMultipleChoiceStatsAsync(int projectId, int questionId);
    Task<LikertStats> ReadLikertStatsAsync(int projectId, int questionId);
    Task<OpenAnswerPage> ReadOpenAnswerPageAsync(int projectId, int questionId, int skip, int take);
    Task<IReadOnlyList<QuestionLookup>> ReadQuestionsByFormatAsync(int projectId, QuestionFormat format);
}
