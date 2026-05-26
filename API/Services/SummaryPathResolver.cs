using Microsoft.Extensions.Options;
using SynapseEnqueteManagement.API.Options;
using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;

namespace SynapseEnqueteManagement.API.Services;

public sealed class SummaryPathResolver
{
    public string BaseDirectory { get; }

    public SummaryPathResolver(IOptions<SummaryStorageOptions> options, IWebHostEnvironment env)
    {
        BaseDirectory = !string.IsNullOrWhiteSpace(options.Value.BaseDirectory)
            ? Path.GetFullPath(options.Value.BaseDirectory)
            : Path.Combine(env.ContentRootPath, "uploads", "summaries");
    }

    public void EnsureDirectoryExists() => Directory.CreateDirectory(BaseDirectory);

    public string ResolvePath(IdeaSummaryJob job)
    {
        var ext = job.ExportFormat switch
        {
            IdeaSummaryExportFormat.Pdf => "pdf",
            IdeaSummaryExportFormat.Png => "png",
            _                          => "txt",
        };
        return Path.GetFullPath(Path.Combine(BaseDirectory, $"{job.IdeaSummaryJobId}.{ext}"));
    }

    public (string ContentType, string FileName) GetDownloadMeta(IdeaSummaryJob job) =>
        job.ExportFormat switch
        {
            IdeaSummaryExportFormat.Pdf => ("application/pdf",          $"summary-{job.ProjectId}.pdf"),
            IdeaSummaryExportFormat.Png => ("image/png",                $"summary-{job.ProjectId}.png"),
            _                          => ("text/plain; charset=utf-8", $"summary-{job.ProjectId}.txt"),
        };
}