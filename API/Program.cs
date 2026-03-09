using Google.Cloud.AIPlatform.V1;
using Google.Cloud.VertexAI.Extensions;
using Microsoft.EntityFrameworkCore;
using SynapseEnqueteManagement.BL;
using SynapseEnqueteManagement.BL.AiModeration;
using SynapseEnqueteManagement.DAL;

var UserUiHttpPort = 5291;
var UserUiHttpsPort = 7163;
bool deleteDatabaseIfExists = true;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Registering DAL
//TODO this shouldn't be hardcoded, but change based on Environment (Factory Pattern?)
builder.Services.AddDbContext<SynapseEnqueteContext>(options => options.UseSqlite("Data Source=SynapseEnquete.db"));
builder.Services.AddScoped<DbContext>(sp => sp.GetRequiredService<SynapseEnqueteContext>());
builder.Services.AddScoped<IRepository, Repository>();

// Registering BL
builder.Services.AddScoped<IQuestionnaireManager, QuestionnaireManager>();
builder.Services.AddScoped<IParticipantManager, ParticipantManager>();
builder.Services.AddScoped<IResponseManager, ResponseManager>();
builder.Services.AddScoped<ICategoryManager, CategoryManager>();
builder.Services.AddScoped<IProjectManager, ProjectManager>();

// Registering AI
var projectId = builder.Configuration["VertexAi:ProjectId"]
    ?? throw new InvalidOperationException("Missing configuration: VertexAi:ProjectId");

var location = builder.Configuration["VertexAi:Location"]
    ?? throw new InvalidOperationException("Missing configuration: VertexAi:Location");

var model = builder.Configuration["VertexAi:Model"]
    ?? "gemini-2.5-flash-lite";

var endpoint = builder.Configuration["VertexAi:Endpoint"]
    ?? $"{location}-aiplatform.googleapis.com";

var predictionBuilder = new PredictionServiceClientBuilder
{ 
    Endpoint = endpoint
    };

var chatModelResource = EndpointName.FormatProjectLocationPublisherModel(
        projectId, location, "google", model);

var chatClient = await predictionBuilder.BuildIChatClientAsync(chatModelResource);

builder.Services.AddChatClient(chatClient);
builder.Services.AddScoped<IAiModerationService, GoogleVertexAiService>();

// CORS - UI-User must access this API
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowUiUser", policy =>
    {
        policy.WithOrigins("http://localhost:" + UserUiHttpPort, "https://localhost:" + UserUiHttpsPort)
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
    context.CreateDatabase(deleteDatabaseIfExists);
    DummyDataSeeder.Seed(context);
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors("AllowUiUser");
app.UseAuthorization();
app.MapControllers();
app.Run();