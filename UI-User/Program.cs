using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Vite.AspNetCore;
using SynapseEnqueteManagement.UI.User.Filters;
using SynapseEnqueteManagement.UI.User.Middleware;
using SynapseEnqueteManagement.UI.User.Services.IdeaImage;
using SynapseEnqueteManagement.UI.User.Services.Ideation;
using SynapseEnqueteManagement.UI.User.Services.Moderation;
using SynapseEnqueteManagement.UI.User.Services.SpeechToText;
using SynapseEnqueteManagement.UI.User.Services.Project;
using SynapseEnqueteManagement.UI.User.Services.Session;
using SynapseEnqueteManagement.UI.User.Services.Survey;
using SynapseEnqueteManagement.UI.User.Filters.FormStrategies;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute());
});
builder.Services.AddHttpLogging(options =>
{
    options.LoggingFields = Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.RequestMethod
                          | Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.RequestPath
                          | Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.ResponseStatusCode
                          | Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.Duration;
});

var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "https://localhost:5159";
var apiKey = builder.Configuration["ApiKey"]
             ?? throw new InvalidOperationException("ApiKey is not configured. Set it in appsettings.json.");
var isDev = builder.Environment.IsDevelopment();

void ConfigureApiClient(HttpClient client)
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
}

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
builder.Services.AddHttpClient<ParticipantService>(ConfigureApiClient)
    .ConfigurePrimaryHttpMessageHandler(CreateHandler);
builder.Services.AddHttpClient<IdeaService>(ConfigureApiClient)
    .ConfigurePrimaryHttpMessageHandler(CreateHandler);
builder.Services.AddHttpClient<CommentService>(ConfigureApiClient)
    .ConfigurePrimaryHttpMessageHandler(CreateHandler);
builder.Services.AddHttpClient<EmojiService>(ConfigureApiClient)
    .ConfigurePrimaryHttpMessageHandler(CreateHandler);
builder.Services.AddHttpClient<ModerationService>(ConfigureApiClient)
    .ConfigurePrimaryHttpMessageHandler(CreateHandler);
builder.Services.AddHttpClient<IdeaSupportService>(ConfigureApiClient)
    .ConfigurePrimaryHttpMessageHandler(CreateHandler);
builder.Services.AddHttpClient<CommentSupportService>(ConfigureApiClient)
    .ConfigurePrimaryHttpMessageHandler(CreateHandler);
builder.Services.AddHttpClient<SpeechToTextService>(ConfigureApiClient)
    .ConfigurePrimaryHttpMessageHandler(CreateHandler);
builder.Services.AddHttpClient<IIdeaImageService, IdeaImageService>(ConfigureApiClient)
    .ConfigurePrimaryHttpMessageHandler(CreateHandler);

builder.Services.AddMemoryCache();

// Rate limiting — per real client IP, applied only to expensive AI moderation endpoints
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("ai-moderation", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                Window      = TimeSpan.FromMinutes(1),
                PermitLimit = 20,
                QueueLimit  = 0,
            }));
    options.AddPolicy("ai-comment-support", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                Window      = TimeSpan.FromMinutes(1),
                PermitLimit = 20,
                QueueLimit  = 0,
            }));
    options.AddPolicy("ai-stt", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                Window      = TimeSpan.FromMinutes(1),
                PermitLimit = 10,
                QueueLimit  = 0,
            }));
    options.AddPolicy("ai-image-moderation", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                Window      = TimeSpan.FromMinutes(1),
                PermitLimit = 10,
                QueueLimit  = 0,
            }));
    options.AddPolicy("participant-creation", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                Window      = TimeSpan.FromMinutes(1),
                PermitLimit = 5,
                QueueLimit  = 0,
            }));
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

builder.Services.AddScoped<RequiresSurveyCompletionFilter>();

// Form strategies (OCP: register a new IFormStrategy here to add a new form type)
builder.Services.AddSingleton<IFormStrategy, ScrollFormStrategy>();
builder.Services.AddSingleton<IFormStrategy, ChatFormStrategy>();
builder.Services.AddSingleton<IFormStrategyFactory, FormStrategyFactory>();

// ---- Service Configuration ----
// Add Vite services.
builder.Services.AddViteServices();

var app = builder.Build();

// Cloud Run (and other reverse proxies) terminate HTTPS and forward HTTP.
// This middleware reads X-Forwarded-For/Proto headers so Request.IsHttps
// and Request.Scheme reflect the original client request.
var forwardedHeadersOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
};
forwardedHeadersOptions.KnownNetworks.Clear();
forwardedHeadersOptions.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedHeadersOptions);

app.UseMiddleware<ProjectShortFormResolverMiddleware>();

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
app.UseHttpLogging();
app.UseStaticFiles();
app.UseRouting();
app.UseRateLimiter();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "project",
    pattern: "{projectId:int}/{controller=ProjectLandingPage}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.Run();
