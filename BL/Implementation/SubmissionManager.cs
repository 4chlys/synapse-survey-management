using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Interfaces;

namespace SynapseEnqueteManagement.BL.Implementation;

public class SubmissionManager(
    IQuestionnaireManager questionnaireManager,
    IResponseManager responseManager,
    IParticipantManager participantManager) : ISubmissionManager
{
    public async Task SubmitAsync(int projectId, Participant participant, CancellationToken ct = default)
    {
        var questionnaire = await questionnaireManager.GetQuestionnaireByProjectIdAsync(projectId)
                            ?? throw new KeyNotFoundException($"Questionnaire for project {projectId} not found.");

        var visiblePath = await questionnaireManager.ComputeVisiblePathAsync(projectId, participant.ParticipantId);
        var visibleSet = visiblePath.ToHashSet();

        var responses = await responseManager.GetResponsesForParticipantAsync(participant.ParticipantId);
        var answeredPairs = responses.Select(r => (r.Question.QuestionId, r.Value));

        questionnaireManager.ValidateMandatoryAnswers(questionnaire, answeredPairs, visibleSet);

        await responseManager.CommitResponsesAsync(participant.ParticipantId, visibleSet, ct);
        await participantManager.MarkAsSubmittedAsync(participant.SessionToken, ct);
    }
}
