using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.DAL.Stats;

namespace SynapseEnqueteManagement.DAL.Interfaces;

public interface IResponseRepository : IRepository<Response>
{
    Task<Response> ReadByQuestionAndParticipantReadOnlyAsync(int questionId, int participantId);

    Task<Response> ReadByQuestionAndParticipantForUpdateAsync(int questionId, int participantId);

    Task<IEnumerable<Response>> ReadByParticipantWithQuestionForUpdateAsync(int participantId);

    Task<IEnumerable<Response>> ReadByParticipantWithQuestionTranslationsForUpdateAsync(int participantId);

    Task<IEnumerable<Response>> ReadByParticipantWithQuestionReadOnlyAsync(int participantId);

    Task DeleteAllByParticipantIdAsync(int participantId);

    Task<IEnumerable<Response>> ReadCommittedByProjectWithQuestionReadOnlyAsync(int projectId);

    Task<IReadOnlyList<OptionCount>> ReadCountsBySingleChoiceQuestionReadOnlyAsync(int questionId);

    Task<IReadOnlyList<OptionCount>> ReadCountsByMultipleChoiceQuestionReadOnlyAsync(int questionId);

    Task<IReadOnlyList<LikertPointCount>> ReadLikertDistributionReadOnlyAsync(int questionId);

    Task<IReadOnlyList<OpenAnswer>> ReadOpenAnswersReadOnlyAsync(int questionId, int skip, int take);

    Task<int> CountResponsesByQuestionAsync(int questionId);
}
