using SynapseEnqueteManagement.API.BackgroundServices;
using SynapseEnqueteManagement.API.Options;
using SynapseEnqueteManagement.API.Services;
using SynapseEnqueteManagement.BL.Ai;
using SynapseEnqueteManagement.BL.AiIdeaSummary;
using SynapseEnqueteManagement.BL.AiModeration;
using SynapseEnqueteManagement.BL.AiUsage;
using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Implementation;
using SynapseEnqueteManagement.BL.InputValidation;
using SynapseEnqueteManagement.BL.Interfaces;
using SynapseEnqueteManagement.BL.Sorting;
using SynapseEnqueteManagement.BL.Sorting.Embeddings;
using SynapseEnqueteManagement.BL.Stats;
using SynapseEnqueteManagement.DAL.Implementation;
using SynapseEnqueteManagement.DAL.Interfaces;

namespace SynapseEnqueteManagement.API.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<ILanguageRepository, LanguageRepository>();
        services.AddScoped<IIdeaRepository, IdeaRepository>();
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<IEmojiRepository, EmojiRepository>();
        services.AddScoped<IResponseRepository, ResponseRepository>();
        services.AddScoped<IQuestionStatsRepository, QuestionStatsRepository>();
        services.AddScoped<IParticipantRepository, ParticipantRepository>();
        services.AddScoped<IQuestionnaireRepository, QuestionnaireRepository>();
        services.AddScoped<IQuestionRepository, QuestionRepository>();
        services.AddScoped<ISectionRepository, SectionRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<ICommentRepository, CommentRepository>();
        services.AddScoped<IProjectPlatformRepository, ProjectPlatformRepository>();
        services.AddScoped<IEmailJobRepository, EmailJobRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        return services;
    }

    public static IServiceCollection AddSortStrategies(this IServiceCollection services)
    {
        // Sort strategies (singleton — stateless)
        services.AddSingleton<ISortStrategy<Idea>, RandomSortStrategy<Idea>>();
        services.AddSingleton<ISortStrategyFactory<Idea>>(sp =>
            new SortStrategyFactory<Idea>(sp.GetServices<ISortStrategy<Idea>>(), defaultKey: "random"));

        services.AddSingleton<ISortStrategy<TextComment>, OldestFirstSortStrategy<TextComment>>();
        services.AddSingleton<ISortStrategyFactory<TextComment>>(sp =>
            new SortStrategyFactory<TextComment>(sp.GetServices<ISortStrategy<TextComment>>(), defaultKey: "oldest"));

        // Embedding-based sort strategies (in-memory; not expressible as SQL ORDER BY)
        services.AddSingleton<IEmbeddingSortStrategy, SimilarSortStrategy>();
        services.AddSingleton<IEmbeddingSortStrategy, DifferentSortStrategy>();
        services.AddSingleton<IEmbeddingSortStrategy, DiverseSortStrategy>();
        services.AddSingleton<IEmbeddingSortStrategyFactory, EmbeddingSortStrategyFactory>();

        // Response validators (OCP: add new formats by registering a new IResponseValidator)
        services.AddSingleton<IResponseValidator, OpenResponseValidator>();
        services.AddSingleton<IResponseValidator, SingleChoiceResponseValidator>();
        services.AddSingleton<IResponseValidator, MultipleChoiceResponseValidator>();
        services.AddSingleton<IResponseValidator, LikertResponseValidator>();
        services.AddSingleton<IResponseValidatorFactory, ResponseValidatorFactory>();

        return services;
    }

    public static IServiceCollection AddManagers(this IServiceCollection services, IConfiguration configuration)
    {
        // Registering BL
        var defaultReserved = new[] { "admin", "api", "www", "app", "mail", "static", "assets", "cdn", "auth", "health" };
        var reservedSubdomains = configuration.GetSection("Subdomains:Reserved").Get<string[]>() ?? defaultReserved;
        services.AddScoped<IShortFormService>(sp =>
            new ShortFormService(sp.GetRequiredService<IProjectRepository>(), reservedSubdomains));
        services.AddScoped<ILanguageManager, LanguageManager>();
        services.AddScoped<ILanguageResolver, LanguageResolver>();
        services.AddScoped<IEmojiManager, EmojiManager>();
        services.AddScoped<IProjectTranslationValidator, ProjectTranslationValidator>();
        services.AddScoped<IProjectActivationValidator, ProjectActivationValidator>();
        services.AddScoped<IQuestionnaireManager, QuestionnaireManager>();
        services.AddScoped<ISectionManager, SectionManager>();
        services.AddScoped<IParticipantManager, ParticipantManager>();
        services.AddScoped<IResponseManager, ResponseManager>();
        services.AddScoped<ICategoryManager, CategoryManager>();
        services.AddScoped<IProjectManager, ProjectManager>();
        services.AddScoped<IProjectPlatformManager, ProjectPlatformManager>();
        services.AddScoped<IIdeaManager, IdeaManager>();
        services.AddScoped<IProjectDuplicationService, ProjectDuplicationService>();
        services.AddScoped<IDashboardManager, DashboardManager>();
        services.AddScoped<IModerationManager, ModerationManager>();
        services.AddScoped<ICommentManager, CommentManager>();
        services.AddScoped<IModerationResolver, ModerationResolver>();
        services.AddScoped<ISurveyFlowManager, SurveyFlowManager>();
        services.AddScoped<ISubmissionManager, SubmissionManager>();
        services.AddScoped<IIdeaSubmissionManager, IdeaSubmissionManager>();
        services.AddScoped<IParticipantLanguageCoordinator, ParticipantLanguageCoordinator>();
        services.AddScoped<IQuestionStatsManager, QuestionStatsManager>();
        services.AddScoped<IEmailJobManager, EmailJobManager>();
        return services;
    }

    public static IServiceCollection AddExportPipeline(this IServiceCollection services)
    {
        services.AddScoped<IQuestionMediaCleanupService, QuestionMediaCleanupService>();
        services.AddSingleton<IExportService, ExportService>();
        services.AddSingleton<IChartImageService, ScottPlotChartImageService>();
        services.AddScoped<IChartRendererService, XlsxChartRendererService>();
        return services;
    }

    public static IServiceCollection AddAiServices(this IServiceCollection services, IConfiguration configuration, AiConfig aiConfig)
    {
        // AI usage tracking
        services.Configure<AiPricingConfig>(configuration.GetSection("AiPricing"));
        services.AddSingleton<AiPricing>();
        services.AddScoped<IAiUsageRepository, AiUsageRepository>();
        services.AddScoped<IAiUsageManager, AiUsageManager>();
        services.AddSingleton<ISystemStatsAggregator, SystemStatsAggregator>();

        // Idea summary
        services.Configure<SummaryStorageOptions>(configuration.GetSection("SummaryStorage"));
        services.AddSingleton<SummaryPathResolver>();
        services.AddScoped<IIdeaSummaryJobRepository, IdeaSummaryJobRepository>();
        services.AddScoped<IIdeaSummaryJobManager, IdeaSummaryJobManager>();
        services.AddScoped<IAiIdeaSummaryService, AiIdeaSummaryService>();
        services.AddSingleton<IIdeaSummarySamplingService, IdeaSummarySamplingService>();
        services.AddSingleton<IdeaSummaryExporter>();
        services.AddHostedService<IdeaSummaryWorker>();

        // Outbound email queue worker — non-AI but co-located with the other
        // BackgroundService for symmetry. Repo + manager are registered above
        // in AddRepositories / AddManagers.
        services.AddHostedService<EmailJobWorker>();

        // AI configuration
        //
        // Register the already-loaded AiConfig instance directly so we don't parse
        // the file a second time. BannedWordList and RegexModerationPatterns are
        // derived from AiConfig and are stateless after construction.
        //
        // To apply changes to ai-config.json, restart the API.
        services.AddSingleton(aiConfig);
        services.AddSingleton(aiConfig.Moderation);
        services.AddSingleton(aiConfig.Fuzzy);
        services.AddSingleton(aiConfig.ImageGeneration);
        services.AddSingleton(aiConfig.ImageDescription);
        services.AddSingleton(aiConfig.SpeechToText);
        services.AddSingleton(aiConfig.SocialMedia);
        services.AddSingleton<BannedWordList>();
        services.AddSingleton<RegexModerationPatterns>();
        services.AddSingleton<IRegexModerationService, RegexModerationService>();
        services.AddSingleton<IInputValidationService, InputValidationService>();
        return services;
    }
}
