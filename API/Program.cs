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
using SynapseEnqueteManagement.API.Middleware;
using SynapseEnqueteManagement.BL;
using SynapseEnqueteManagement.BL.Implementation;
using SynapseEnqueteManagement.BL.Interfaces;
using SynapseEnqueteManagement.BL.AiModeration;
using SynapseEnqueteManagement.DAL;

bool deleteIfExists = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";

// Load AI config early so we can read use_ai before building the container
var tempLogger = LoggerFactory.Create(b => b.AddConsole()).CreateLogger<Program>();
var aiConfig = AiConfigLoader.Load(tempLogger);
bool useAi = aiConfig.Moderation.UseAi;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddOpenApi();
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
builder.Services.AddScoped<IRepository, Repository>();

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

// Registering BL
builder.Services.AddScoped<IQuestionnaireManager, QuestionnaireManager>();
builder.Services.AddScoped<IParticipantManager, ParticipantManager>();
builder.Services.AddScoped<IResponseManager, ResponseManager>();
builder.Services.AddScoped<ICategoryManager, CategoryManager>();
builder.Services.AddScoped<IProjectManager, ProjectManager>();
builder.Services.AddScoped<IIdeaManager, IdeaManager>();
builder.Services.AddScoped<ICommentManager, CommentManager>();
builder.Services.AddScoped<IModerationResolver, ModerationResolver>();

// AI configuration
//
// Register the already-loaded AiConfig instance directly so we don't parse
// the file a second time. BannedWordList and RegexModerationPatterns are
// derived from AiConfig and are stateless after construction.
//
// To apply changes to ai-config.json, restart the API.
builder.Services.AddSingleton(aiConfig);
builder.Services.AddSingleton<BannedWordList>();
builder.Services.AddSingleton<RegexModerationPatterns>();

// Moderation token service (signed nonce — stateless across instances)
var tokenSecret = builder.Configuration["ApiKey"]
    ?? throw new InvalidOperationException("ApiKey is not configured. Set it in .env or as an environment variable.");

builder.Services.AddSingleton<IModerationTokenService>(sp =>
    new ModerationTokenService(tokenSecret, sp.GetRequiredService<ILogger<ModerationTokenService>>()));

// Vertex AI (optional — controlled by use_ai in ai-config.json)
if (useAi)
{
    var projectId = builder.Configuration["VertexAi:ProjectId"]
                    ?? throw new InvalidOperationException("Missing VertexAi:ProjectId");
    var location = builder.Configuration["VertexAi:Location"] ?? "europe-west1";
    var model = builder.Configuration["VertexAi:Model"] ?? "gemini-2.5-flash-lite";
    var embeddingModel = builder.Configuration["VertexAi:EmbeddingModel"] ?? "gemini-embedding-001";
    var endpoint = builder.Configuration["VertexAi:Endpoint"] ?? $"{location}-aiplatform.googleapis.com";

    var predictionBuilder = new PredictionServiceClientBuilder { Endpoint = endpoint };
    var chatModelResource = EndpointName.FormatProjectLocationPublisherModel(
        projectId, location, "google", model);
    var chatClient = await predictionBuilder.BuildIChatClientAsync(chatModelResource);

    builder.Services.AddChatClient(chatClient);
    builder.Services.AddScoped<IAiModerationService, GoogleVertexAiService>();

    // Embedding generator — same PredictionServiceClientBuilder, different model
    // resource. AiEmbeddingService consumes the abstract IEmbeddingGenerator, so
    // swapping Vertex for OpenAI/Ollama later means registering a different
    // IEmbeddingGenerator here; nothing in BL changes.
    var embeddingModelResource = EndpointName.FormatProjectLocationPublisherModel(
        projectId, location, "google", embeddingModel);
    var embeddingGenerator = await predictionBuilder.BuildIEmbeddingGeneratorAsync(embeddingModelResource);
    builder.Services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(embeddingGenerator);
    builder.Services.AddSingleton<IEmbeddingService, AiEmbeddingService>();
}
else
{
    builder.Services.AddScoped<IAiModerationService, MockModerationService>();
    builder.Services.AddSingleton<IEmbeddingService, MockEmbeddingService>();
}

// CORS — not needed: UI-User calls API server-side via HttpClient, not from the browser.
// Re-enable if a browser-based client ever needs direct API access.

var app = builder.Build();

// Database initialisation & seeding
// Retry loop: needed for Cloud Run cold starts where the postgres sidecar
// is not yet ready when the API boots (race condition).
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<SynapseEnqueteContext>();
    var dbLogger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    const int maxRetries = 10;
    const int delayMs = 3000;

    for (int attempt = 1; attempt <= maxRetries; attempt++)
    {
        try
        {
            context.CreateDatabase(deleteIfExists);
            var seedEmbedSvc = scope.ServiceProvider.GetRequiredService<IEmbeddingService>();
            DummyDataSeeder.Seed(context, text => seedEmbedSvc.GenerateAsync(text).GetAwaiter().GetResult());
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

if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();

app.UseHttpLogging();
app.UseMiddleware<ExceptionHandlerMiddleware>();
app.UseMiddleware<ApiKeyMiddleware>();
app.UseAuthorization();

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