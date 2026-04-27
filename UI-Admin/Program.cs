using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SynapseEnqueteManagement.UI.Admin.Data;
using SynapseEnqueteManagement.UI.Admin.Services.Category;
using SynapseEnqueteManagement.UI.Admin.Services.Dashboard;
using SynapseEnqueteManagement.UI.Admin.Services.Platform;
using SynapseEnqueteManagement.UI.Admin.Services.Project;
using SynapseEnqueteManagement.UI.Admin.Services.Question;
using SynapseEnqueteManagement.UI.Admin.Services.Section;
using Vite.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute());
});

// AdminDbContext — Identity + ProjectPlatform (shared DB)
var dbHost = Environment.GetEnvironmentVariable("DB_HOST");
if (!string.IsNullOrEmpty(dbHost))
{
    var connStr = $"Host={dbHost};" +
                  $"Port={Environment.GetEnvironmentVariable("DB_PORT") ?? "5432"};" +
                  $"Database={Environment.GetEnvironmentVariable("DB_NAME")};" +
                  $"Username={Environment.GetEnvironmentVariable("DB_USER")};" +
                  $"Password={Environment.GetEnvironmentVariable("DB_PASSWORD")}";
    builder.Services.AddDbContext<AdminDbContext>(opt => opt.UseNpgsql(connStr));
}
else
{
    var connStr = builder.Configuration.GetConnectionString("AdminDb")
                  ?? "Data Source=../API/SynapseEnquete.db";
    builder.Services.AddDbContext<AdminDbContext>(opt => opt.UseSqlite(connStr));
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

// API HTTP clients (unchanged from original)
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
builder.Services.AddHttpClient<SectionAdminService>(ConfigureApiClient)
    .ConfigurePrimaryHttpMessageHandler(CreateHandler);
builder.Services.AddHttpClient<DashboardAdminService>(ConfigureApiClient)
    .ConfigurePrimaryHttpMessageHandler(CreateHandler);
builder.Services.AddHttpClient<StatsProxyService>(ConfigureApiClient)
    .ConfigurePrimaryHttpMessageHandler(CreateHandler);
builder.Services.AddHttpClient<IdeaAdminService>(ConfigureApiClient)
    .ConfigurePrimaryHttpMessageHandler(CreateHandler);

builder.Services.AddScoped<PlatformAdminService>();
builder.Services.AddScoped<AdminSeeder>();
builder.Services.AddScoped<WidgetPreferenceService>();

builder.Services.AddViteServices();

var app = builder.Build();

// Apply Identity migrations + seed roles and system admin
using (var scope = app.Services.CreateScope())
{
    var adminDb = scope.ServiceProvider.GetRequiredService<AdminDbContext>();
    await adminDb.Database.MigrateAsync();

    var seeder = scope.ServiceProvider.GetRequiredService<AdminSeeder>();
    await seeder.SeedAsync();
}

if (app.Environment.IsDevelopment())
    app.UseViteDevelopmentServer();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.MapStaticAssets();
app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Project}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
