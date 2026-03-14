using Vite.AspNetCore;
using SynapseEnqueteManagement.UI.User.Services;
using SynapseEnqueteManagement.UI.User.FormStrategies;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient<IQuestionnaireApiService, QuestionnaireApiService>(client =>
     {
         client.BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"]
             ?? "https://localhost:5159");
     })
    .ConfigurePrimaryHttpMessageHandler(() =>
    {
        var handler = new HttpClientHandler();
        if (builder.Environment.IsDevelopment())
            handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
        return handler;
    });

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ISessionService, CookieSessionService>();

builder.Services.AddHttpClient<CategoryService>(client =>
    {
        client.BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"]
            ?? "https://localhost:5159");
    })
    .ConfigurePrimaryHttpMessageHandler(() =>
    {
        var handler = new HttpClientHandler();
        if (builder.Environment.IsDevelopment())
            handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
        return handler;
    });
builder.Services.AddHttpClient<IProjectApiService, ProjectApiService>(client =>
    {
        client.BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"]
            ?? "https://localhost:5159");
    })
    .ConfigurePrimaryHttpMessageHandler(() =>
    {
        var handler = new HttpClientHandler();
        if (builder.Environment.IsDevelopment())
            handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
        return handler;
    });

builder.Services.AddHttpClient<IdeaService>(client =>
    {
        client.BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"]
                                     ?? "https://localhost:5159");
    })
    .ConfigurePrimaryHttpMessageHandler(() =>
    {
        var handler = new HttpClientHandler();
        if (builder.Environment.IsDevelopment())
            handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
        return handler;
    });

builder.Services.AddHttpClient<EmojiService>(client =>
    {
        client.BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"]
                                     ?? "https://localhost:5159");
    })
    .ConfigurePrimaryHttpMessageHandler(() =>
    {
        var handler = new HttpClientHandler();
        if (builder.Environment.IsDevelopment())
            handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
        return handler;
    });

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
    pattern: "{projectId:int}/{controller=ProjectLandingPage}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapGet("/", () => Results.Redirect("/1"));

app.Run();