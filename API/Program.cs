using Microsoft.EntityFrameworkCore;
using SynapseEnqueteManagement.BL;
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