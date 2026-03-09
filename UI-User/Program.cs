using Vite.AspNetCore;
using SynapseEnqueteManagement.UI.User.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient<IQuestionnaireApiService, QuestionnaireApiService>(client =>
     {
         client.BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"]
             ?? "https://localhost:5159");
     })
    .ConfigurePrimaryHttpMessageHandler(() =>
    {
        // Accept the dev SSL certificate for server-to-server calls
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
builder.Services.AddHttpClient<ProjectApiService>(client =>
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

// ---- Service Configuration ----
// Add Vite services.
builder.Services.AddViteServices();

var app = builder.Build();
// ---- App Configuration ----
// Use the Vite Development Server when the environment is Development.
if (app.Environment.IsDevelopment())
{
    // WebSockets support is required for HMR (hot module reload).
    // Uncomment the following line if your pipeline doesn't contain it.
    // app.UseWebSockets();
    // Enable all required features to use the Vite Development Server.
    // Pass true if you want to use the integrated middleware.
    app.UseViteDevelopmentServer(/* false */);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "project",
    pattern: "{projectId:int}/{controller=Survey}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapGet("/", () => Results.Redirect("/1/Survey"));

app.Run();
