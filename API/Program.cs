using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Pgvector.EntityFrameworkCore;
using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Sorting;
using SynapseEnqueteManagement.BL.Sorting.Embeddings;
using SynapseEnqueteManagement.BL.Embeddings;
using Google.Cloud.AIPlatform.V1;
using Google.Cloud.VertexAI.Extensions;
using Microsoft.Extensions.AI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SynapseEnqueteManagement.API.Ai;
using SynapseEnqueteManagement.API.Data;
using SynapseEnqueteManagement.BL.Domain.Ai;
using SynapseEnqueteManagement.API.Middleware;
using SynapseEnqueteManagement.API.Services;
using SynapseEnqueteManagement.BL;
using SynapseEnqueteManagement.BL.Ai;
using SynapseEnqueteManagement.BL.AiCommentSupport;
using SynapseEnqueteManagement.BL.AiIdeaSupport;
using SynapseEnqueteManagement.BL.AiModeration;
using SynapseEnqueteManagement.BL.AiImageDescription;
using SynapseEnqueteManagement.BL.AiImageGeneration;
using SynapseEnqueteManagement.BL.AiQuestionSupport;
using SynapseEnqueteManagement.BL.AiSpeechToText;
using SynapseEnqueteManagement.BL.Implementation;
using SynapseEnqueteManagement.BL.InputValidation;
using SynapseEnqueteManagement.BL.Interfaces;
using SynapseEnqueteManagement.BL.AiUsage;
using SynapseEnqueteManagement.DAL;
using SynapseEnqueteManagement.DAL.Interfaces;
using SynapseEnqueteManagement.DAL.Implementation;
using Resend;
using SynapseEnqueteManagement.BL.AiSocialMedia;

bool deleteIfExists = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";

// Load AI config early so we can read use_ai before building the container
var tempLogger = LoggerFactory.Create(b => b.AddConsole()).CreateLogger<Program>();
var aiConfig = AiConfigLoader.Load(tempLogger);
bool useAi = aiConfig.Moderation.UseAi;

var builder = WebApplication.CreateBuilder(args);

// Allow up to 30 MB request bodies (question video uploads are capped at 25 MB at the action level,
// leaving headroom for multipart overhead). Cloud Run HTTP/1 hard-caps at 32 MB — do not raise further.
builder.WebHost.ConfigureKestrel(o => o.Limits.MaxRequestBodySize = 30 * 1024 * 1024);
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(
    o => o.MultipartBodyLengthLimit = 30 * 1024 * 1024);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddLocalization(opts => opts.ResourcesPath = "Resources");

var apiSupportedCultures = new[] { "nl", "en" };
builder.Services.Configure<Microsoft.AspNetCore.Builder.RequestLocalizationOptions>(opts =>
{
    opts.SetDefaultCulture("nl")
        .AddSupportedCultures(apiSupportedCultures)
        .AddSupportedUICultures(apiSupportedCultures);
    opts.RequestCultureProviders =
    [
        new Microsoft.AspNetCore.Localization.AcceptLanguageHeaderRequestCultureProvider()
    ];
});
builder.Services.AddHttpLogging(options =>
{
    options.LoggingFields = Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.RequestMethod
                            | Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.RequestPath
                            | Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.ResponseStatusCode
                            | Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.Duration;
});

// Database
var dbHost = Environment.GetEnvironmentVariable("DB_HOST");
if (!string.IsNullOrEmpty(dbHost))
{
    var connStr = $"Host={dbHost};" +
                  $"Port={Environment.GetEnvironmentVariable("DB_PORT") ?? "5432"};" +
                  $"Database={Environment.GetEnvironmentVariable("DB_NAME")};" +
                  $"Username={Environment.GetEnvironmentVariable("DB_USER")};" +
                  $"Password={Environment.GetEnvironmentVariable("DB_PASSWORD")}";
    builder.Services.AddDbContext<SynapseEnqueteContext>(options => options.UseNpgsql(connStr, o => o.UseVector()));
}
else
{
    builder.Services.AddDbContext<SynapseEnqueteContext>(options => options.UseSqlite("Data Source=SynapseEnquete.db"));
}

builder.Services.AddScoped<DbContext>(sp => sp.GetRequiredService<SynapseEnqueteContext>());

// AdminReadContext — reads PlatformSettings + SystemAiSettings from the shared DB.
// DEPLOY ORDER: UI-Admin must run its migrations before the API starts, otherwise
// these tables do not yet exist and all AI endpoints will fail at runtime.
if (!string.IsNullOrEmpty(dbHost))
{
    var adminConnStr = $"Host={dbHost};" +
                       $"Port={Environment.GetEnvironmentVariable("DB_PORT") ?? "5432"};" +
                       $"Database={Environment.GetEnvironmentVariable("DB_NAME")};" +
                       $"Username={Environment.GetEnvironmentVariable("DB_USER")};" +
                       $"Password={Environment.GetEnvironmentVariable("DB_PASSWORD")}";
    builder.Services.AddDbContext<AdminReadContext>(opt => opt.UseNpgsql(adminConnStr));
}
else
{
    var adminConnStr = builder.Configuration.GetConnectionString("AdminDb")
                       ?? "Data Source=../UI-Admin/SynapseEnquete.db";
    builder.Services.AddDbContext<AdminReadContext>(opt => opt.UseSqlite(adminConnStr));
}

builder.Services.AddMemoryCache();
builder.Services.AddScoped<IPlatformAiConfigService, PlatformAiConfigService>();
builder.Services.AddScoped<ILanguageRepository, LanguageRepository>();
builder.Services.AddScoped<IIdeaRepository, IdeaRepository>();
builder.Services.AddScoped<IProjectRepository, ProjectRepository>();
builder.Services.AddScoped<IEmojiRepository, EmojiRepository>();
builder.Services.AddScoped<IResponseRepository, ResponseRepository>();
builder.Services.AddScoped<IQuestionStatsRepository, QuestionStatsRepository>();
builder.Services.AddScoped<IParticipantRepository, ParticipantRepository>();
builder.Services.AddScoped<IQuestionnaireRepository, QuestionnaireRepository>();
builder.Services.AddScoped<IQuestionRepository, QuestionRepository>();
builder.Services.AddScoped<ISectionRepository, SectionRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<ICommentRepository, CommentRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Sort strategies (singleton — stateless)
builder.Services.AddSingleton<ISortStrategy<Idea>, RandomSortStrategy<Idea>>();
builder.Services.AddSingleton<ISortStrategyFactory<Idea>>(sp =>
    new SortStrategyFactory<Idea>(sp.GetServices<ISortStrategy<Idea>>(), defaultKey: "random"));

builder.Services.AddSingleton<ISortStrategy<TextComment>, OldestFirstSortStrategy<TextComment>>();
builder.Services.AddSingleton<ISortStrategyFactory<TextComment>>(sp =>
    new SortStrategyFactory<TextComment>(sp.GetServices<ISortStrategy<TextComment>>(), defaultKey: "oldest"));

// Embedding-based sort strategies (in-memory; not expressible as SQL ORDER BY)
builder.Services.AddSingleton<IEmbeddingSortStrategy, SimilarSortStrategy>();
builder.Services.AddSingleton<IEmbeddingSortStrategy, DifferentSortStrategy>();
builder.Services.AddSingleton<IEmbeddingSortStrategy, DiverseSortStrategy>();
builder.Services.AddSingleton<IEmbeddingSortStrategyFactory, EmbeddingSortStrategyFactory>();

// IEmbeddingService is registered below inside/after the Vertex-AI block: the
// real backing IEmbeddingGenerator<string, Embedding<float>> only exists when
// useAi=true, so Mock is the fallback when AI is disabled.

// Response validators (OCP: add new formats by registering a new IResponseValidator)
builder.Services.AddSingleton<IResponseValidator, OpenResponseValidator>();
builder.Services.AddSingleton<IResponseValidator, SingleChoiceResponseValidator>();
builder.Services.AddSingleton<IResponseValidator, MultipleChoiceResponseValidator>();
builder.Services.AddSingleton<IResponseValidator, LikertResponseValidator>();
builder.Services.AddSingleton<IResponseValidatorFactory, ResponseValidatorFactory>();

// Storage
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<IStorageService, LocalStorageService>();
}
else
{
    builder.Services.AddSingleton<IStorageService, GoogleCloudStorageService>();
}

builder.Services.AddSingleton<IExportService, ExportService>();
builder.Services.AddSingleton<IChartImageService, ScottPlotChartImageService>();
builder.Services.AddScoped<IChartRendererService, XlsxChartRendererService>();

// Registering BL
var defaultReserved = new[] { "admin", "api", "www", "app", "mail", "static", "assets", "cdn", "auth", "health" };
var reservedSubdomains = builder.Configuration.GetSection("Subdomains:Reserved").Get<string[]>() ?? defaultReserved;
builder.Services.AddScoped<IShortFormService>(sp =>
    new ShortFormService(sp.GetRequiredService<IProjectRepository>(), reservedSubdomains));
builder.Services.AddScoped<ILanguageManager, LanguageManager>();
builder.Services.AddScoped<IQuestionnaireManager, QuestionnaireManager>();
builder.Services.AddScoped<IParticipantManager, ParticipantManager>();
builder.Services.AddScoped<IResponseManager, ResponseManager>();
builder.Services.AddScoped<ICategoryManager, CategoryManager>();
builder.Services.AddScoped<IProjectManager, ProjectManager>();
builder.Services.AddScoped<IIdeaManager, IdeaManager>();
builder.Services.AddScoped<IProjectDuplicationService, ProjectDuplicationService>();
builder.Services.AddScoped<IDashboardManager, DashboardManager>();
builder.Services.AddScoped<IModerationManager, ModerationManager>();
builder.Services.AddScoped<ICommentManager, CommentManager>();
builder.Services.AddScoped<IModerationResolver, ModerationResolver>();
builder.Services.AddScoped<ISurveyFlowManager, SurveyFlowManager>();
builder.Services.AddScoped<ISubmissionManager, SubmissionManager>();
builder.Services.AddScoped<IParticipantLanguageCoordinator, ParticipantLanguageCoordinator>();
builder.Services.AddScoped<IQuestionStatsManager, QuestionStatsManager>();

// Rate limiting — cap email sends per source so a leaked API key can't be used to
// blast mail through our Resend account / sender domain.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("admin-email-send", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                Window      = TimeSpan.FromMinutes(1),
                PermitLimit = 10,
                QueueLimit  = 0,
            }));
});

// Email
var resendApiKey = builder.Configuration["RESEND_API_KEY"];
var emailFrom = builder.Configuration["EMAIL_FROM"] ?? "noreply@synapse-ip.xyz";
if (!string.IsNullOrEmpty(resendApiKey))
{
    builder.Services.AddResend(options => options.ApiToken = resendApiKey);
    builder.Services.AddScoped<IEmailService>(sp =>
        new SynapseEnqueteManagement.BL.Services.ResendEmailService(sp.GetRequiredService<IResend>(), emailFrom));
}
else
{
    builder.Services.AddScoped<IEmailService, SynapseEnqueteManagement.BL.Services.LogEmailService>();
}

// AI usage tracking
builder.Services.Configure<AiPricingConfig>(builder.Configuration.GetSection("AiPricing"));
builder.Services.AddSingleton<AiPricing>();
builder.Services.AddScoped<IAiUsageRepository, AiUsageRepository>();
builder.Services.AddScoped<IAiUsageManager, AiUsageManager>();

// AI configuration
//
// Register the already-loaded AiConfig instance directly so we don't parse
// the file a second time. BannedWordList and RegexModerationPatterns are
// derived from AiConfig and are stateless after construction.
//
// To apply changes to ai-config.json, restart the API.
builder.Services.AddSingleton(aiConfig);
builder.Services.AddSingleton(aiConfig.Moderation);
builder.Services.AddSingleton(aiConfig.Fuzzy);
builder.Services.AddSingleton(aiConfig.ImageGeneration);
builder.Services.AddSingleton(aiConfig.ImageDescription);
builder.Services.AddSingleton(aiConfig.SpeechToText);
builder.Services.AddSingleton(aiConfig.SocialMedia);
builder.Services.AddSingleton<BannedWordList>();
builder.Services.AddSingleton<RegexModerationPatterns>();
builder.Services.AddSingleton<IRegexModerationService, RegexModerationService>();
builder.Services.AddSingleton<IInputValidationService, InputValidationService>();

// Moderation token service (signed nonce — stateless across instances)
var tokenSecret = builder.Configuration["ApiKey"]
                  ?? throw new InvalidOperationException(
                      "ApiKey is not configured. Set it in .env or as an environment variable.");

builder.Services.AddSingleton<IModerationTokenService>(sp =>
    new ModerationTokenService(tokenSecret, sp.GetRequiredService<ILogger<ModerationTokenService>>()));

// AI chat clients — multi-provider registry (optional — controlled by use_ai in ai-config.json)
if (useAi)
{
    var projectId    = builder.Configuration["VertexAi:ProjectId"]
                       ?? throw new InvalidOperationException("Missing VertexAi:ProjectId");
    var location     = builder.Configuration["VertexAi:Location"] ?? "europe-west1";
    var embeddingModel = builder.Configuration["VertexAi:EmbeddingModel"] ?? "gemini-embedding-001";
    var endpoint     = builder.Configuration["VertexAi:Endpoint"] ?? $"{location}-aiplatform.googleapis.com";

    var predictionBuilder = new PredictionServiceClientBuilder { Endpoint = endpoint };

    var chatClients = new Dictionary<string, IChatClient>();

    // Vertex AI models
    foreach (var vertexModel in AiModelCatalog.All.Where(m => m.Provider == AiProvider.VertexAi))
    {
        try
        {
            var resource = EndpointName.FormatProjectLocationPublisherModel(projectId, location, "google", vertexModel.Id);
            var client   = await predictionBuilder.BuildIChatClientAsync(resource);
            chatClients[vertexModel.Id] = client;
            tempLogger.LogInformation("[AI] Registered Vertex AI model: {Model}", vertexModel.Id);
        }
        catch (Exception ex)
        {
            tempLogger.LogWarning(ex, "[AI] Failed to register Vertex AI model {Model} — skipping.", vertexModel.Id);
        }
    }

    // OpenAI
    var openAiKey = builder.Configuration["OpenAi:ApiKey"];
    if (!string.IsNullOrWhiteSpace(openAiKey))
    {
        foreach (var openAiModel in AiModelCatalog.All.Where(m => m.Provider == AiProvider.OpenAi))
        {
            var openAiClient = new OpenAI.OpenAIClient(openAiKey);
            chatClients[openAiModel.Id] = openAiClient.GetChatClient(openAiModel.Id).AsIChatClient();
            tempLogger.LogInformation("[AI] Registered OpenAI model: {Model}", openAiModel.Id);
        }
    }

    // Anthropic
    var anthropicKey = builder.Configuration["Anthropic:ApiKey"];
    if (!string.IsNullOrWhiteSpace(anthropicKey))
    {
        foreach (var anthropicModel in AiModelCatalog.All.Where(m => m.Provider == AiProvider.Anthropic))
        {
            var anthropicClient = new Anthropic.SDK.AnthropicClient(anthropicKey);
            chatClients[anthropicModel.Id] = anthropicClient.Messages;
            tempLogger.LogInformation("[AI] Registered Anthropic model: {Model}", anthropicModel.Id);
        }
    }

    // Grok (OpenAI-compatible)
    var xaiKey = builder.Configuration["Grok:ApiKey"];
    if (!string.IsNullOrWhiteSpace(xaiKey))
    {
        foreach (var grokModel in AiModelCatalog.All.Where(m => m.Provider == AiProvider.Grok))
        {
            var grokClient = new OpenAI.OpenAIClient(
                new System.ClientModel.ApiKeyCredential(xaiKey),
                new OpenAI.OpenAIClientOptions { Endpoint = new Uri("https://api.x.ai/v1") });
            chatClients[grokModel.Id] = grokClient.GetChatClient(grokModel.Id).AsIChatClient();
            tempLogger.LogInformation("[AI] Registered Grok model: {Model}", grokModel.Id);
        }
    }

    var defaultModelId = builder.Configuration["VertexAi:Model"];
    builder.Services.AddSingleton(new ChatClientRegistry(chatClients, defaultModelId));
    builder.Services.AddSingleton<ModelAvailabilityService>();

    // All AI features share the ChatClientRegistry.
    builder.Services.AddScoped<IAiModerationService, AiModerationService>();
    builder.Services.AddScoped<IAiIdeaSupportService, AiIdeaSupportService>();
    builder.Services.AddScoped<IAiCommentSupportService, AiCommentSupportService>();
    builder.Services.AddScoped<IAiQuestionSupportService, AiQuestionSupportService>();
    builder.Services.AddScoped<IAiSpeechToTextService, AiSpeechToTextService>();
    builder.Services.AddScoped<IAiImageDescriptionService, AiImageDescriptionService>();
    builder.Services.AddScoped<IAiSocialPostService, AiSocialPostService>();

    // Image generation — Imagen uses PredictAsync directly, not IChatClient.
    var imagenModel = aiConfig.ImageGeneration.Model;
    var imagenModelResource = EndpointName.FormatProjectLocationPublisherModel(
        projectId, location, "google", imagenModel);
    var imagenClient = await predictionBuilder.BuildAsync();
    builder.Services.AddSingleton<IAiImageGenerationService>(sp =>
        new AiImageGenerationService(
            imagenClient,
            imagenModelResource,
            sp.GetRequiredService<AiImageGenerationConfig>(),
            sp.GetRequiredService<ILogger<AiImageGenerationService>>()));

    // Embedding generator — same PredictionServiceClientBuilder, different model
    // resource. AiEmbeddingService consumes the abstract IEmbeddingGenerator, so
    // swapping Vertex for OpenAI/Ollama later means registering a different
    // IEmbeddingGenerator here; nothing in BL changes.
    var embeddingModelResource = EndpointName.FormatProjectLocationPublisherModel(
        projectId, location, "google", embeddingModel);
    var embeddingGenerator = await predictionBuilder.BuildIEmbeddingGeneratorAsync(embeddingModelResource);
    builder.Services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(embeddingGenerator);
    builder.Services.AddSingleton(new AiEmbeddingModelOptions { Model = embeddingModel });
    builder.Services.AddScoped<IEmbeddingService, AiEmbeddingService>();
}
else
{
    builder.Services.AddSingleton(new ChatClientRegistry(new Dictionary<string, IChatClient>()));
    builder.Services.AddSingleton<ModelAvailabilityService>();
    builder.Services.AddScoped<IAiModerationService, MockModerationService>();
    builder.Services.AddScoped<IEmbeddingService, MockEmbeddingService>();
    builder.Services.AddScoped<IAiIdeaSupportService, MockIdeaSupportService>();
    builder.Services.AddScoped<IAiCommentSupportService, MockCommentSupportService>();
    builder.Services.AddScoped<IAiQuestionSupportService, MockQuestionSupportService>();
    builder.Services.AddScoped<IAiSpeechToTextService, MockSpeechToTextService>();
    builder.Services.AddScoped<IAiImageDescriptionService, MockImageDescriptionService>();
    builder.Services.AddSingleton<IAiImageGenerationService, MockImageGenerationService>();
    builder.Services.AddSingleton<IAiSocialPostService, MockSocialPostService>();
}

// CORS — not needed: UI-User calls API server-side via HttpClient, not from the browser.
// Re-enable if a browser-based client ever needs direct API access.

var app = builder.Build();

// Database initialisation & seeding
// Retry loop: needed for Cloud Run cold starts where the postgres sidecar
// is not yet ready when the API boots (race condition).
// Scope is recreated per attempt so a failed attempt's stale ChangeTracker
// can't leak into the next attempt's SaveChanges.
{
    const int maxRetries = 10;
    const int delayMs = 3000;

    for (int attempt = 1; attempt <= maxRetries; attempt++)
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<SynapseEnqueteContext>();
        var dbLogger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        try
        {
            if (context.Database.IsNpgsql())
                context.Database.ExecuteSqlRaw("CREATE EXTENSION IF NOT EXISTS vector;");
            context.CreateDatabase(deleteIfExists);
            var apiBaseUrl = builder.Configuration["ApiBaseUrl"];
            DummyDataSeeder.Seed(context, apiBaseUrl);

            try
            {
                var seedEmbedSvc = scope.ServiceProvider.GetRequiredService<IEmbeddingService>();
                var ideasNeedingEmbeddings = await context.Set<Idea>()
                    .Where(i => i.Embedding == null)
                    .ToListAsync();
                foreach (var idea in ideasNeedingEmbeddings)
                    idea.Embedding = await seedEmbedSvc.GenerateAsync(idea.Description, 0);
                if (ideasNeedingEmbeddings.Count > 0)
                    await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                dbLogger.LogWarning(ex, "[Seed] Embedding backfill skipped; embeddings will be generated on first use.");
            }
            break;
        }
        catch (Exception ex) when (attempt < maxRetries)
        {
            dbLogger.LogWarning("Database not ready (attempt {Attempt}/{Max}): {Message}. Retrying in {Delay}ms...",
                attempt, maxRetries, ex.Message, delayMs);
            await Task.Delay(delayMs);
        }
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedFor
});

if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseHttpLogging();
app.UseRequestLocalization();
app.UseMiddleware<ExceptionHandlerMiddleware>();
app.UseMiddleware<ApiKeyMiddleware>();
app.UseAuthorization();
app.UseRateLimiter();

app.MapGet("/health", async (SynapseEnqueteContext db) =>
{
    try
    {
        await db.Database.CanConnectAsync();
        return Results.Ok(new { status = "healthy" });
    }
    catch
    {
        return Results.StatusCode(503);
    }
});

app.MapControllers();
app.Run();