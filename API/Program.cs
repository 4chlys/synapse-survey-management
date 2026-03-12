using Google.Cloud.AIPlatform.V1;
using Google.Cloud.VertexAI.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SynapseEnqueteManagement.BL;
using SynapseEnqueteManagement.BL.AiModeration;
using SynapseEnqueteManagement.DAL;

var UserUiHttpPort = 5291;
var UserUiHttpsPort = 7163;

bool deleteIfExists = true;

bool useAI = true;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// ── Database ────────────────────────────────────────────────────────────────
var dbHost = Environment.GetEnvironmentVariable("DB_HOST");
if (!string.IsNullOrEmpty(dbHost))
{
    var connStr = $"Host={dbHost};" +
                  $"Port={Environment.GetEnvironmentVariable("DB_PORT") ?? "5432"};" +
                  $"Database={Environment.GetEnvironmentVariable("DB_NAME")};" +
                  $"Username={Environment.GetEnvironmentVariable("DB_USER")};" +
                  $"Password={Environment.GetEnvironmentVariable("DB_PASSWORD")}";
    builder.Services.AddDbContext<SynapseEnqueteContext>(o => o.UseNpgsql(connStr));
}
else
{
    builder.Services.AddDbContext<SynapseEnqueteContext>(
        o => o.UseSqlite("Data Source=SynapseEnquete.db"));
}

builder.Services.AddScoped<DbContext>(sp => sp.GetRequiredService<SynapseEnqueteContext>());
builder.Services.AddScoped<IRepository, Repository>();

// ── Business logic ──────────────────────────────────────────────────────────
builder.Services.AddScoped<IQuestionnaireManager, QuestionnaireManager>();
builder.Services.AddScoped<IParticipantManager, ParticipantManager>();
builder.Services.AddScoped<IResponseManager, ResponseManager>();
builder.Services.AddScoped<ICategoryManager, CategoryManager>();
builder.Services.AddScoped<IProjectManager, ProjectManager>();

// ── AI configuration (loaded from ai-config.json / ai-config.example.json) ─
//
// AiConfig is a singleton: it is read once at startup and shared for the
// lifetime of the process. BannedWordList and RegexModerationPatterns are
// also singletons because they are derived from AiConfig and are stateless
// after construction.
//
// To apply changes to ai-config.json, restart the API.
builder.Services.AddSingleton(sp =>
{
    var logger = sp.GetRequiredService<ILogger<Program>>();
    return AiConfigLoader.Load(logger);
});
builder.Services.AddSingleton<BannedWordList>();
builder.Services.AddSingleton<RegexModerationPatterns>();

// ── Vertex AI (optional) ────────────────────────────────────────────────────
if (useAI)
{
    var projectId = builder.Configuration["VertexAi:ProjectId"]
                    ?? throw new InvalidOperationException("Missing VertexAi:ProjectId");
    var location  = builder.Configuration["VertexAi:Location"] ?? "europe-west1";
    var model     = builder.Configuration["VertexAi:Model"]    ?? "gemini-2.5-flash-lite";
    var endpoint  = builder.Configuration["VertexAi:Endpoint"] ?? $"{location}-aiplatform.googleapis.com";

    var predictionBuilder = new PredictionServiceClientBuilder { Endpoint = endpoint };
    var chatModelResource = EndpointName.FormatProjectLocationPublisherModel(
        projectId, location, "google", model);
    var chatClient = await predictionBuilder.BuildIChatClientAsync(chatModelResource);

    builder.Services.AddChatClient(chatClient);
    builder.Services.AddScoped<IAiModerationService, GoogleVertexAiService>();
}

// ── CORS ────────────────────────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowUiUser", policy =>
    {
        policy.WithOrigins(
            "http://localhost:5291",
            "https://localhost:7163",
            Environment.GetEnvironmentVariable("UI_URL") ??
            "http://localhost:5291"
        );
    });
});

var app = builder.Build();


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<SynapseEnqueteContext>();
    context.CreateDatabase(deleteIfExists);
    DummyDataSeeder.Seed(context);
    app.MapOpenApi();
}

if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();

app.UseCors("AllowUiUser");
app.UseAuthorization();
app.MapControllers();
app.Run();