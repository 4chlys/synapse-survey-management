using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.BL.AiIdeaSummary;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;
using SynapseEnqueteManagement.BL.Interfaces;

namespace SynapseEnqueteManagement.API.Controllers;

[ApiController]
[Route("api/admin/projects/{projectId:int}/ideas/summary")]
public class AdminIdeaSummaryController(
    IIdeaSummaryJobManager jobs,
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
            return BadRequest(new { error = $"Invalid export format '{req.Format}'." });

        if (!Enum.TryParse<IdeaSummarySamplingStrategy>(req.SamplingStrategy, ignoreCase: true, out var strategy))
            return BadRequest(new { error = $"Invalid sampling strategy '{req.SamplingStrategy}'." });

        var filterJson = JsonSerializer.Serialize(new SummaryFilterParams
        {
            CategoryId       = req.CategoryId,
            Status           = req.Status,
            LanguageCode     = req.LanguageCode,
            OutputLanguage   = req.OutputLanguage,
            SamplingStrategy = strategy,
            SelectAll        = req.SelectAll,
        });

        var job = await jobs.EnqueueAsync(
            projectId, platformId, format, filterJson,
            User.Identity?.Name, TimeSpan.FromMinutes(5), ct);

        return Ok(new { jobId = job.IdeaSummaryJobId });
    }

    [HttpGet("{jobId:guid}/status")]
    public async Task<IActionResult> GetStatus(int projectId, Guid jobId, CancellationToken ct)
    {
        var job = await jobs.GetForProjectAsync(projectId, jobId, ct);
        if (job is null) return NotFound();

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
        var job = await jobs.GetForProjectAsync(projectId, jobId, ct);
        if (job is null) return NotFound();

        if (job.Status != IdeaSummaryJobStatus.Completed || job.OutputPath is null)
            return BadRequest(new { error = "Summary is not ready yet." });

        var webRoot  = env.WebRootPath ?? string.Empty;
        var fullPath = Path.GetFullPath(Path.Combine(webRoot, job.OutputPath));
        if (!fullPath.StartsWith(webRoot, StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { error = "Invalid file path." });
        if (!System.IO.File.Exists(fullPath)) return NotFound();

        var contentType = Path.GetExtension(job.OutputPath).ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            ".png" => "image/png",
            _      => "text/plain; charset=utf-8",
        };
        var bytes = await System.IO.File.ReadAllBytesAsync(fullPath, ct);
        return File(bytes, contentType, Path.GetFileName(job.OutputPath));
    }
}

public sealed record EnqueueSummaryRequest(
    string Format,
    string SamplingStrategy,
    int? CategoryId,
    string Status,
    string LanguageCode,
    string OutputLanguage,
    bool SelectAll);