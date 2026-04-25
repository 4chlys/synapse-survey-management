using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.BL.Interfaces;
using SynapseEnqueteManagement.BL.Domain.Project.Entities;

namespace SynapseEnqueteManagement.API.Controllers;

public abstract class ParticipantControllerBase(
    IParticipantManager participantManager,
    IProjectManager projectManager) : ControllerBase
{
    private const string SessionHeader = "X-Participant-Token";

    protected async Task<(Participant participant, ActionResult error)> GetParticipantAsync()
    {
        if (!TryGetParticipantToken(out var token))
            return (null, Unauthorized("Missing or invalid X-Participant-Token header."));

        var participant = await participantManager.GetParticipantBySessionTokenForUpdateAsync(token);
        if (participant is null)
            return (null, NotFound("Participant session not found."));

        return (participant, null);
    }

    protected Task<Project> GetProjectByIdReadOnlyAsync(int projectId)
        => projectManager.GetProjectByIdReadOnlyAsync(projectId);

    protected async Task<Participant> AddParticipantAsync(int projectId, CancellationToken ct = default)
    {
        var project = await projectManager.GetProjectByIdForUpdateAsync(projectId)
                      ?? throw new KeyNotFoundException($"Project {projectId} not found.");
        return await participantManager.AddParticipantAsync(project, ct);
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
        return await participantManager.GetParticipantBySessionTokenForUpdateAsync(token);
    }

    protected async Task<bool> HasParticipantSubmittedAsync(Guid token)
    {
        return await participantManager.HasParticipantSubmittedAsync(token);
    }

    protected async Task MarkAsSubmittedAsync(Guid sessionToken, CancellationToken ct = default)
    {
        await participantManager.MarkAsSubmittedAsync(sessionToken, ct);
    }

    protected async Task SaveContactInfoAsync(int participantId, string email, CancellationToken ct = default)
    {
        await participantManager.SaveContactInfoAsync(participantId, email, ct);
    }

    protected bool TryGetParticipantToken(out Guid token)
    {
        token = Guid.Empty;
        if (!Request.Headers.TryGetValue(SessionHeader, out var headerValue))
            return false;

        return Guid.TryParse(headerValue.ToString(), out token);
    }
}
