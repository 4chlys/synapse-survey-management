using Google.Cloud.AIPlatform.V1;
using Google.Cloud.VertexAI.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SynapseEnqueteManagement.BL;
using SynapseEnqueteManagement.BL.AiModeration;
using SynapseEnqueteManagement.DAL;

bool deleteIfExists = true;

// Load AI config early so we can read use_ai before building the container
var tempLogger = LoggerFactory.Create(b => b.AddConsole()).CreateLogger<Program>();
var aiConfig = AiConfigLoader.Load(tempLogger);
bool useAi = aiConfig.Moderation.UseAi;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Database
var dbHost = Environment.GetEnvironmentVariable("DB_HOST");
if (!string.IsNullOrEmpty(dbHost))
{
    var connStr = $"Host={dbHost};" +
                  $"Port={Environment.GetEnvironmentVariable("DB_PORT") ?? "5432"};" +
                  $"Database={Environment.GetEnvironmentVariable("DB_NAME")};" +
                  $"Username={Environment.GetEnvironmentVariable("DB_USER")};" +
                  $"Password={Environment.GetEnvironmentVariable("DB_PASSWORD")}";
    builder.Services.AddDbContext<SynapseEnqueteContext>(options => options.UseNpgsql(connStr));
}
else
{
    builder.Services.AddDbContext<SynapseEnqueteContext>(options => options.UseSqlite("Data Source=SynapseEnquete.db"));
}

builder.Services.AddScoped<DbContext>(sp => sp.GetRequiredService<SynapseEnqueteContext>());
builder.Services.AddScoped<IRepository, Repository>();

// Registering BL
builder.Services.AddScoped<IQuestionnaireManager, QuestionnaireManager>();
builder.Services.AddScoped<IParticipantManager, ParticipantManager>();
builder.Services.AddScoped<IResponseManager, ResponseManager>();
builder.Services.AddScoped<ICategoryManager, CategoryManager>();
builder.Services.AddScoped<IProjectManager, ProjectManager>();
builder.Services.AddScoped<IIdeaManager, IdeaManager>();
builder.Services.AddScoped<ICommentManager, CommentManager>();

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

// Vertex AI (optional — controlled by use_ai in ai-config.json)
if (useAi)
{
    var projectId = builder.Configuration["VertexAi:ProjectId"]
                    ?? throw new InvalidOperationException("Missing VertexAi:ProjectId");
    var location = builder.Configuration["VertexAi:Location"] ?? "europe-west1";
    var model = builder.Configuration["VertexAi:Model"] ?? "gemini-2.5-flash-lite";
    var endpoint = builder.Configuration["VertexAi:Endpoint"] ?? $"{location}-aiplatform.googleapis.com";

    var predictionBuilder = new PredictionServiceClientBuilder { Endpoint = endpoint };
    var chatModelResource = EndpointName.FormatProjectLocationPublisherModel(
        projectId, location, "google", model);
    var chatClient = await predictionBuilder.BuildIChatClientAsync(chatModelResource);

    builder.Services.AddChatClient(chatClient);
    builder.Services.AddScoped<IAiModerationService, GoogleVertexAiService>();
} else builder.Services.AddScoped<IAiModerationService, MockModerationService>();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowUiUser", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:5291",
                "https://localhost:7163",
                Environment.GetEnvironmentVariable("UI_URL") ?? "http://localhost:5291"
            )
            .AllowAnyHeader()
            .AllowAnyMethod();
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