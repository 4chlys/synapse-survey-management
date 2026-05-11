using SynapseEnqueteManagement.BL.Domain.Project.Entities;

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
}
