using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.API.Services;
using SynapseEnqueteManagement.BL.Interfaces;

namespace SynapseEnqueteManagement.API.Controllers;

[ApiController]
[Route("api/admin/projects/{projectId:int}/participants")]
public class AdminParticipantsController(
    IParticipantManager participantManager,
    IProjectManager projectManager,
    IExportService exportService) : ControllerBase
{
    [HttpGet("emails/export")]
    public async Task<IActionResult> ExportEmails(int projectId, [FromQuery] string format = "csv")
    {
        if (await projectManager.GetProjectByIdReadOnlyAsync(projectId) is null)
            return NotFound();

        var normalized = (format ?? "csv").Trim().ToLowerInvariant();
        if (normalized is not ("csv" or "txt" or "xlsx"))
            return BadRequest(new { error = $"Ongeldig formaat '{format}'." });

        var emails = (await participantManager.GetParticipantEmailsByProjectAsync(projectId)).ToList();
        var emailRows = emails.Select(e => new[] { e });

        var (bytes, contentType, fileName) = normalized switch
        {
            "csv"  => (exportService.BuildCsv(["Email"], emailRows),  "text/csv", $"participants-{projectId}.csv"),
            "txt"  => (exportService.BuildTxt(emails),  "text/plain", $"participants-{projectId}.txt"),
            "xlsx" => (exportService.BuildXlsx(["Email"], emailRows, "Participants"), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"participants-{projectId}.xlsx"),
            _ => throw new InvalidOperationException()
        };

        return File(bytes, contentType, fileName);
    }

    // Used by the admin "Send to all participants" broadcast. UI-Admin
    // groups the returned rows by LanguageCode, then enqueues one EmailJob
    // per language × ≤49-BCC chunk (Resend's per-message recipient cap).
    [HttpGet("emails-by-language")]
    public async Task<IActionResult> EmailsByLanguage(int projectId, CancellationToken ct)
    {
        if (await projectManager.GetProjectByIdReadOnlyAsync(projectId) is null)
            return NotFound();

        var rows = await participantManager.GetParticipantEmailsWithLanguageByProjectAsync(projectId);
        return Ok(rows);
    }
}
