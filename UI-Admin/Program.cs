using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Options;
using SynapseEnqueteManagement.UI.Admin;
using SynapseEnqueteManagement.UI.Admin.Data;
using SynapseEnqueteManagement.UI.Admin.Services;
using SynapseEnqueteManagement.UI.Admin.Services.Category;
using SynapseEnqueteManagement.UI.Admin.Services.Dashboard;
using SynapseEnqueteManagement.UI.Admin.Services.Platform;
using SynapseEnqueteManagement.UI.Admin.Services.Project;
using SynapseEnqueteManagement.UI.Admin.Services.Question;
using SynapseEnqueteManagement.UI.Admin.Services.Section;
using SynapseEnqueteManagement.UI.Admin.Services.QuestionSupport;
using SynapseEnqueteManagement.UI.Admin.Services.Upload;
using SynapseEnqueteManagement.UI.Admin.Services.Ai;
using SynapseEnqueteManagement.UI.Admin.Services.IdeaSummary;
using SynapseEnqueteManagement.UI.Admin.Services.Email;
using Vite.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddTransient<CultureForwardingHandler>();
builder.Services.ConfigureHttpClientDefaults(b => b.AddHttpMessageHandler<CultureForwardingHandler>());

builder.Services.AddLocalization(opts => opts.ResourcesPath = "Resources");

var supportedCultures = new[] { "nl", "en" };
builder.Services.Configure<RequestLocalizationOptions>(opts =>
{
    opts.SetDefaultCulture("nl")
        .AddSupportedCultures(supportedCultures)
        .AddSupportedUICultures(supportedCultures);

    opts.RequestCultureProviders =
    [
        new CookieRequestCultureProvider(),
        new AcceptLanguageHeaderRequestCultureProvider()
    ];
});

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute());
})
.AddViewLocalization()
.AddDataAnnotationsLocalization();

// AdminDbContext — Identity + ProjectPlatform (shared DB)
var dbHost = Environment.GetEnvironmentVariable("DB_HOST");
if (!string.IsNullOrEmpty(dbHost))
{
    var connStr = $"Host={dbHost};" +
                  $"Port={Environment.GetEnvironmentVariable("DB_PORT") ?? "5432"};" +
                  $"Database={Environment.GetEnvironmentVariable("DB_NAME")};" +
                  $"Username={Environment.GetEnvironmentVariable("DB_USER")};" +
                  $"Password={Environment.GetEnvironmentVariable("DB_PASSWORD")}";
    builder.Services.AddDbContext<AdminDbContext>(opt => opt
        .UseNpgsql(connStr)
        .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning)));
}
else
{
    var connStr = builder.Configuration.GetConnectionString("AdminDb")
                  ?? "Data Source=../API/SynapseEnquete.db";
    builder.Services.AddDbContext<AdminDbContext>(opt => opt
        .UseSqlite(connStr)
        .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning)));
}

// Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.Password.RequireDigit = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = false;
    })
    .AddEntityFrameworkStores<AdminDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Auth/Login";
    options.AccessDeniedPath = "/Auth/Login";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
});

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// API HTTP clients
var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "https://localhost:7128";
var apiKey = builder.Configuration["ApiKey"]
             ?? throw new InvalidOperationException("ApiKey is not configured.");
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

builder.Services.AddHttpClient<ProjectAdminService>(ConfigureApiClient)
    .ConfigurePrimaryHttpMessageHandler(CreateHandler);
builder.Services.AddHttpClient<QuestionAdminService>(ConfigureApiClient)
    .ConfigurePrimaryHttpMessageHandler(CreateHandler);
builder.Services.AddHttpClient<CategoryAdminService>(ConfigureApiClient)
    .ConfigurePrimaryHttpMessageHandler(CreateHandler);
builder.Services.AddHttpClient<EmojiAdminService>(ConfigureApiClient)
    .ConfigurePrimaryHttpMessageHandler(CreateHandler);
builder.Services.AddHttpClient<SectionAdminService>(ConfigureApiClient)
    .ConfigurePrimaryHttpMessageHandler(CreateHandler);
builder.Services.AddHttpClient<DashboardAdminService>(ConfigureApiClient)
    .ConfigurePrimaryHttpMessageHandler(CreateHandler);
builder.Services.AddHttpClient<StatsProxyService>(ConfigureApiClient)
    .ConfigurePrimaryHttpMessageHandler(CreateHandler);
builder.Services.AddHttpClient<IdeaAdminService>(ConfigureApiClient)
    .ConfigurePrimaryHttpMessageHandler(CreateHandler);
builder.Services.AddHttpClient<ParticipantAdminService>(ConfigureApiClient)
    .ConfigurePrimaryHttpMessageHandler(CreateHandler);
builder.Services.AddHttpClient<UploadAdminService>(ConfigureApiClient)
    .ConfigurePrimaryHttpMessageHandler(CreateHandler);
builder.Services.AddHttpClient<QuestionSupportService>(ConfigureApiClient)
    .ConfigurePrimaryHttpMessageHandler(CreateHandler);
builder.Services.AddHttpClient<SocialMediaProxyService>(ConfigureApiClient)
    .ConfigurePrimaryHttpMessageHandler(CreateHandler);
builder.Services.AddHttpClient<IQuestionMediaService, QuestionMediaService>(ConfigureApiClient)
    .ConfigurePrimaryHttpMessageHandler(CreateHandler);
builder.Services.AddHttpClient<AiAdminService>(ConfigureApiClient)
    .ConfigurePrimaryHttpMessageHandler(CreateHandler);
builder.Services.AddHttpClient<IdeaSummaryAdminService>(ConfigureApiClient)
    .ConfigurePrimaryHttpMessageHandler(CreateHandler);
builder.Services.AddHttpClient<SystemStatsAdminService>(ConfigureApiClient)
    .ConfigurePrimaryHttpMessageHandler(CreateHandler);
builder.Services.AddHttpClient<EmailApiService>(ConfigureApiClient)
    .ConfigurePrimaryHttpMessageHandler(CreateHandler);
builder.Services.AddHttpClient<PlatformApiService>(ConfigureApiClient)
    .ConfigurePrimaryHttpMessageHandler(CreateHandler);

builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("question-media-upload", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                Window      = TimeSpan.FromMinutes(1),
                PermitLimit = 5,
                QueueLimit  = 0,
            }));
});

builder.Services.Configure<AdminSettings>(builder.Configuration.GetSection("AdminSettings"));

builder.Services.AddScoped<PlatformAdminService>();
builder.Services.AddScoped<AdminInviteService>();
builder.Services.AddScoped<AdminSeeder>();
builder.Services.AddScoped<WidgetPreferenceService>();
builder.Services.AddScoped<SynapseEnqueteManagement.UI.Admin.Filters.IProjectOwnershipLookup,
                          SynapseEnqueteManagement.UI.Admin.Filters.ProjectOwnershipLookup>();

// Rate limiting — protect expensive AI image-generation via project creation
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("ai-image-generation", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                Window      = TimeSpan.FromMinutes(1),
                PermitLimit = 10,
                QueueLimit  = 0,
            }));
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    // Cloud Run / GFE proxy IPs are dynamic; clear the default allow-list to trust the LB.
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddViteServices();

var app = builder.Build();

// Apply Identity migrations + seed roles and system admin
// Retry loop for Cloud Run cold starts where postgres sidecar may not be ready yet.
{
    const int maxRetries = 10;
    const int delayMs = 3000;
    var startupLogger = app.Services.GetRequiredService<ILogger<Program>>();

    for (int attempt = 1; attempt <= maxRetries; attempt++)
    {
        try
        {
            using var scope = app.Services.CreateScope();
            var adminDb = scope.ServiceProvider.GetRequiredService<AdminDbContext>();
            await adminDb.Database.MigrateAsync();
            var seeder = scope.ServiceProvider.GetRequiredService<AdminSeeder>();
            await seeder.SeedAsync();
            break;
        }
        catch (Exception ex) when (attempt < maxRetries)
        {
            startupLogger.LogWarning("Admin DB not ready (attempt {Attempt}/{Max}): {Message}. Retrying in {Delay}ms...",
                attempt, maxRetries, ex.Message, delayMs);
            await Task.Delay(delayMs);
        }
    }
}

app.UseForwardedHeaders();

if (app.Environment.IsDevelopment())
    app.UseViteDevelopmentServer();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}
app.UseStaticFiles();
app.UseRequestLocalization(app.Services.GetRequiredService<IOptions<RequestLocalizationOptions>>().Value);
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapStaticAssets();
app.MapGet("/health", () => Results.Ok("healthy")).AllowAnonymous();

app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();

// Expose the implicit Program class to the test project (WebApplicationFactory<Program>).
public partial class Program;
