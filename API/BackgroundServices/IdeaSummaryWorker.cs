using System.Text.Json;
using SynapseEnqueteManagement.API.Ai;
using SynapseEnqueteManagement.API.Models;
using SynapseEnqueteManagement.API.Services;
using SynapseEnqueteManagement.BL.AiIdeaSummary;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;
using SynapseEnqueteManagement.DAL.Interfaces;

namespace SynapseEnqueteManagement.API.BackgroundServices;

public sealed class IdeaSummaryWorker(
    IServiceScopeFactory scopeFactory,
    IWebHostEnvironment env,
    ILogger<IdeaSummaryWorker> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(PollInterval);
        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try { await ProcessNextJobAsync(stoppingToken); }
            catch (OperationCanceledException) { break; }
            catch (Exception ex) { logger.LogError(ex, "[IdeaSummaryWorker] Unhandled error in poll loop."); }
        }
    }

    private async Task ProcessNextJobAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var jobRepo    = scope.ServiceProvider.GetRequiredService<IIdeaSummaryJobRepository>();
        var ideaRepo   = scope.ServiceProvider.GetRequiredService<IIdeaRepository>();
        var configSvc  = scope.ServiceProvider.GetRequiredService<IPlatformAiConfigService>();
        var summarySvc = scope.ServiceProvider.GetRequiredService<IAiIdeaSummaryService>();
        var sampling   = scope.ServiceProvider.GetRequiredService<IIdeaSummarySamplingService>();
        var exporter   = scope.ServiceProvider.GetRequiredService<IdeaSummaryExporter>();

        var job = await jobRepo.ClaimNextPendingAsync(ct);
        if (job is null) return;

        logger.LogInformation("[IdeaSummaryWorker] Processing job {JobId} for project {ProjectId}.",
            job.IdeaSummaryJobId, job.ProjectId);

        try
        {
            var filter = JsonSerializer.Deserialize<SummaryFilterParams>(job.FilterJson)
                ?? throw new InvalidOperationException("Invalid FilterJson.");

            var aiConfig = await configSvc.ResolveIdeaSummaryAsync(job.ProjectPlatformId, ct);

            ModerationStatus? statusEnum = null;
            if (!string.IsNullOrWhiteSpace(filter.Status) &&
                Enum.TryParse<ModerationStatus>(filter.Status, ignoreCase: true, out var s))
                statusEnum = s;

            var allIdeas = await ideaRepo.ReadForSummaryAsync(
                job.ProjectId, filter.CategoryId, statusEnum, filter.LanguageCode, ct);

            int? cap = filter.SelectAll ? null : aiConfig.MaxIdeas;
            var sampled = sampling.Sample(allIdeas, filter.SamplingStrategy, cap);

            string summaryText;
            if (filter.SelectAll && allIdeas.Count > aiConfig.MaxIdeas)
                summaryText = await summarySvc.GenerateBatchSummaryAsync(
                    sampled, aiConfig.ChatClient, aiConfig.ModelId, aiConfig.Prompt,
                    batchSize: 50, job.ProjectId, ct);
            else
                summaryText = await summarySvc.GenerateSummaryAsync(
                    sampled, aiConfig.ChatClient, aiConfig.ModelId, aiConfig.Prompt,
                    job.ProjectId, ct);

            var (bytes, _, fileName) = exporter.Export(summaryText, job.ExportFormat, job.ProjectId);

            var relDir = Path.Combine("uploads", "summaries");
            var absDir = Path.Combine(env.WebRootPath ?? string.Empty, relDir);
            Directory.CreateDirectory(absDir);
            var uniqueFileName = $"{job.IdeaSummaryJobId}_{fileName}";
            await File.WriteAllBytesAsync(Path.Combine(absDir, uniqueFileName), bytes, ct);

            job.OutputPath  = Path.Combine(relDir, uniqueFileName);
            job.Status      = IdeaSummaryJobStatus.Completed;
            job.CompletedAt = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[IdeaSummaryWorker] Job {JobId} failed.", job.IdeaSummaryJobId);
            job.Status       = IdeaSummaryJobStatus.Failed;
            job.ErrorMessage = ex.Message;
            job.CompletedAt  = DateTime.UtcNow;
        }

        await jobRepo.UpdateAsync(job, ct);
    }
}