using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.BL;
using SynapseEnqueteManagement.BL.Domain.Project.Entities;

namespace SynapseEnqueteManagement.API.Controllers;

public abstract class ParticipantControllerBase(IParticipantManager participantManager) : ControllerBase
{
    private const string SessionHeader = "X-Participant-Token";

    protected async Task<(Participant participant, ActionResult error)> GetParticipantAsync()
    {
        if (!TryGetParticipantToken(out var token))
            return (null, Unauthorized("Missing or invalid X-Participant-Token header."));

        var participant = await participantManager.GetParticipantBySessionTokenAsync(token);
        if (participant is null)
            return (null, NotFound("Participant session not found."));

        return (participant, null);
    }

    protected async Task<Participant> AddParticipantAsync(Questionnaire questionnaire, int projectId)
    {
        return await participantManager.AddParticipantAsync(questionnaire, projectId);
    }

    protected async Task<(Participant participant, ActionResult error)> GetParticipantAsync(int projectId)
    {
        var (participant, error) = await GetParticipantAsync();
        if (error is not null) return (null, error);

        if (participant.Project.ProjectId != projectId)
            return (null, Forbid());

        return (participant, null);
    }

    protected async Task<Participant> GetParticipantByTokenAsync(Guid token)
    {
        return await participantManager.GetParticipantBySessionTokenAsync(token);
    }

    protected async Task<bool> HasParticipantRespondedAsync(Guid token)
    {
        return await participantManager.HasParticipantRespondedAsync(token);
    }

    protected bool TryGetParticipantToken(out Guid token)
    {
        token = Guid.Empty;
        if (!Request.Headers.TryGetValue(SessionHeader, out var headerValue))
            return false;

        return Guid.TryParse(headerValue.ToString(), out token);
    }
}
