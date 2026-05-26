using System.Text.Json;
using SynapseEnqueteManagement.API.Ai;
using SynapseEnqueteManagement.API.Services;
using SynapseEnqueteManagement.BL.AiIdeaSummary;
using SynapseEnqueteManagement.BL.Interfaces;

namespace SynapseEnqueteManagement.API.BackgroundServices;

public sealed class IdeaSummaryWorker(
    IServiceScopeFactory scopeFactory,
    SummaryPathResolver resolver,
    IdeaSummaryExporter exporter,
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
        var jobManager = scope.ServiceProvider.GetRequiredService<IIdeaSummaryJobManager>();
        var configSvc  = scope.ServiceProvider.GetRequiredService<IPlatformAiConfigService>();
        var summarySvc = scope.ServiceProvider.GetRequiredService<IAiIdeaSummaryService>();
        var sampling   = scope.ServiceProvider.GetRequiredService<IIdeaSummarySamplingService>();

        var job = await jobManager.ClaimNextPendingAsync(ct);
        if (job is null) return;

        logger.LogInformation("[IdeaSummaryWorker] Processing job {JobId} for project {ProjectId}.",
            job.IdeaSummaryJobId, job.ProjectId);

        try
        {
            var filter = JsonSerializer.Deserialize<SummaryFilterParams>(job.FilterJson)
                ?? throw new InvalidOperationException("Invalid FilterJson.");

            var aiConfig = await configSvc.ResolveIdeaSummaryAsync(job.ProjectPlatformId, ct);

            var allIdeas = await jobManager.GetIdeasForSummaryAsync(job.ProjectId, filter, ct);

            int? cap = filter.SelectAll ? null : aiConfig.MaxIdeas;
            var sampled = sampling.Sample(allIdeas, filter.SamplingStrategy, cap);

            string summaryText;
            if (filter.SelectAll && allIdeas.Count > aiConfig.MaxIdeas)
                summaryText = await summarySvc.GenerateBatchSummaryAsync(
                    sampled, aiConfig.ChatClient, aiConfig.ModelId, aiConfig.Prompt,
                    aiConfig.BatchSize, job.ProjectId, filter.OutputLanguage, aiConfig.MaxSummaryChars, ct);
            else
                summaryText = await summarySvc.GenerateSummaryAsync(
                    sampled, aiConfig.ChatClient, aiConfig.ModelId, aiConfig.Prompt,
                    job.ProjectId, filter.OutputLanguage, aiConfig.MaxSummaryChars, ct);

            var (bytes, _, _) = exporter.Export(summaryText, job.ExportFormat, job.ProjectId);

            resolver.EnsureDirectoryExists();
            await File.WriteAllBytesAsync(resolver.ResolvePath(job), bytes, ct);

            await jobManager.MarkCompletedAsync(job, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "[IdeaSummaryWorker] Job {JobId} failed.", job.IdeaSummaryJobId);
            await jobManager.MarkFailedAsync(job, ex.Message, ct);
        }
    }
}