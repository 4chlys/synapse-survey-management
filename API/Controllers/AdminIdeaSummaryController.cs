using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.API.Models;
using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;
using SynapseEnqueteManagement.DAL.Interfaces;

namespace SynapseEnqueteManagement.API.Controllers;

[ApiController]
[Route("api/admin/projects/{projectId:int}/ideas/summary")]
public class AdminIdeaSummaryController(
    IIdeaSummaryJobRepository jobRepo,
    IWebHostEnvironment env) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Enqueue(
        int projectId,
        [FromQuery] int platformId,
        [FromBody] EnqueueSummaryRequest req,
        CancellationToken ct)
    {
        if (!Enum.TryParse<IdeaSummaryExportFormat>(req.Format, ignoreCase: true, out var format))
            return BadRequest(new { error = $"Ongeldig formaat '{req.Format}'." });

        if (!Enum.TryParse<IdeaSummarySamplingStrategy>(req.SamplingStrategy, ignoreCase: true, out var strategy))
            return BadRequest(new { error = $"Ongeldige strategie '{req.SamplingStrategy}'." });

        var filterJson = JsonSerializer.Serialize(new SummaryFilterParams
        {
            CategoryId       = req.CategoryId,
            Status           = req.Status,
            LanguageCode     = req.LanguageCode,
            SamplingStrategy = strategy,
            SelectAll        = req.SelectAll,
        });

        var job = new IdeaSummaryJob
        {
            ProjectPlatformId = platformId,
            ProjectId         = projectId,
            FilterJson        = filterJson,
            ExportFormat      = format,
            CreatedAt         = DateTime.UtcNow,
            RequestedBy       = User.Identity?.Name,
        };

        var created = await jobRepo.CreateAsync(job, ct);
        return Ok(new { jobId = created.IdeaSummaryJobId });
    }

    [HttpGet("{jobId:guid}/status")]
    public async Task<IActionResult> GetStatus(int projectId, Guid jobId, CancellationToken ct)
    {
        var job = await jobRepo.GetByIdAsync(jobId, ct);
        if (job is null || job.ProjectId != projectId) return NotFound();

        return Ok(new
        {
            status       = job.Status.ToString(),
            errorMessage = job.ErrorMessage,
            hasOutput    = job.OutputPath is not null,
        });
    }

    [HttpGet("{jobId:guid}/download")]
    public async Task<IActionResult> Download(int projectId, Guid jobId, CancellationToken ct)
    {
        var job = await jobRepo.GetByIdAsync(jobId, ct);
        if (job is null || job.ProjectId != projectId) return NotFound();
        if (job.Status != IdeaSummaryJobStatus.Completed || job.OutputPath is null)
            return BadRequest(new { error = "Samenvatting is nog niet klaar." });

        var webRoot = env.WebRootPath ?? string.Empty;
        var fullPath = Path.GetFullPath(Path.Combine(webRoot, job.OutputPath));
        if (!fullPath.StartsWith(webRoot, StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { error = "Ongeldig bestandspad." });
        if (!System.IO.File.Exists(fullPath)) return NotFound();

        var contentType = Path.GetExtension(job.OutputPath).ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            ".png" => "image/png",
            _      => "text/plain; charset=utf-8",
        };
        var fileName = Path.GetFileName(job.OutputPath);
        var bytes = await System.IO.File.ReadAllBytesAsync(fullPath, ct);
        return File(bytes, contentType, fileName);
    }
}

public sealed record EnqueueSummaryRequest(
    string Format,
    string SamplingStrategy,
    int? CategoryId,
    string Status,
    string LanguageCode,
    bool SelectAll);