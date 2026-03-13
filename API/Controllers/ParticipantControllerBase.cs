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

    protected bool TryGetParticipantToken(out Guid token)
    {
        token = Guid.Empty;
        if (!Request.Headers.TryGetValue(SessionHeader, out var headerValue))
            return false;

        return Guid.TryParse(headerValue.ToString(), out token);
    }
}
