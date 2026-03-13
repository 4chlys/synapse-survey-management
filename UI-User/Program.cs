using Vite.AspNetCore;
using SynapseEnqueteManagement.UI.User.Services;
using SynapseEnqueteManagement.UI.User.FormStrategies;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "https://localhost:5159";
var isDev = builder.Environment.IsDevelopment();

void ConfigureApiClient(HttpClient client) =>
    client.BaseAddress = new Uri(apiBaseUrl);

HttpClientHandler CreateHandler()
{
    var handler = new HttpClientHandler();
    if (isDev)
        handler.ServerCertificateCustomValidationCallback =
            HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
    return handler;
}

builder.Services.AddHttpClient<IQuestionnaireApiService, QuestionnaireApiService>(ConfigureApiClient)
    .ConfigurePrimaryHttpMessageHandler(CreateHandler);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ISessionService, CookieSessionService>();

builder.Services.AddHttpClient<CategoryService>(ConfigureApiClient)
    .ConfigurePrimaryHttpMessageHandler(CreateHandler);
builder.Services.AddHttpClient<IProjectApiService, ProjectApiService>(ConfigureApiClient)
    .ConfigurePrimaryHttpMessageHandler(CreateHandler);
builder.Services.AddHttpClient<IdeaService>(ConfigureApiClient)
    .ConfigurePrimaryHttpMessageHandler(CreateHandler);
builder.Services.AddHttpClient<CommentService>(ConfigureApiClient)
    .ConfigurePrimaryHttpMessageHandler(CreateHandler);
builder.Services.AddHttpClient<EmojiService>(ConfigureApiClient)
    .ConfigurePrimaryHttpMessageHandler(CreateHandler);
builder.Services.AddHttpClient<ModerationService>(ConfigureApiClient)
    .ConfigurePrimaryHttpMessageHandler(CreateHandler);

// Form strategies (OCP: register a new IFormStrategy here to add a new form type)
builder.Services.AddSingleton<IFormStrategy, ScrollFormStrategy>();
builder.Services.AddSingleton<IFormStrategy, ChatFormStrategy>();
builder.Services.AddSingleton<IFormStrategyFactory, FormStrategyFactory>();

// ---- Service Configuration ----
// Add Vite services.
builder.Services.AddViteServices();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseViteDevelopmentServer();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseStaticFiles();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
        name: "project",
        pattern: "{projectId:int}/{controller=Survey}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapGet("/", () => Results.Redirect("/1/Survey"));

app.Run();