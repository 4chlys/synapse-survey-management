using Microsoft.EntityFrameworkCore;
using SynapseEnqueteManagement.BL;
using SynapseEnqueteManagement.DAL;

var UserUiHttpPort = 5291;
var UserUiHttpsPort = 7163;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Registering DAL — PostgreSQL in de cloud, SQLite lokaal
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
builder.Services.AddScoped<IManager, Manager>();

// CORS - UI-User must access this API
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
    bool deleteIfExists = string.IsNullOrEmpty(dbHost); // SQLite resetten, PostgreSQL behouden
    context.CreateDatabase(deleteIfExists);
    DummyDataSeeder.Seed(context);
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors("AllowUiUser");
app.UseAuthorization();
app.MapControllers();
app.Run();