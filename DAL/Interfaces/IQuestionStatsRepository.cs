using SynapseEnqueteManagement.DAL.Stats;

namespace SynapseEnqueteManagement.DAL.Interfaces;

public interface IQuestionStatsRepository
{
    Task<IReadOnlyList<OptionCount>> ReadCountsBySingleChoiceQuestionReadOnlyAsync(int questionId);

    Task<IReadOnlyList<OptionCount>> ReadCountsByMultipleChoiceQuestionReadOnlyAsync(int questionId);

    Task<IReadOnlyList<LikertPointCount>> ReadLikertDistributionReadOnlyAsync(int questionId);

    Task<IReadOnlyList<OpenAnswer>> ReadOpenAnswersReadOnlyAsync(int questionId, int skip, int take);

    Task<int> CountResponsesByQuestionAsync(int questionId);
}
