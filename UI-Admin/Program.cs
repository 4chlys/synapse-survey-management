using SynapseEnqueteManagement.UI.Admin.Services.Project;
using SynapseEnqueteManagement.UI.Admin.Services.Question;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new
        Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute());
});

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
var app = builder.Build();

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
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();