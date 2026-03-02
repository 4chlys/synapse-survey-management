// Configure database context options for SQLite

using ConsoleApp1;
using Microsoft.EntityFrameworkCore;
using SynapseEnqueteManagement.BL;
using SynapseEnqueteManagement.DAL;

bool deleteIfAlreadyExists = false;

var optionsBuilder = new DbContextOptionsBuilder<SynapseEnqueteContext>();
// Calculate the path to the solution root directory
// Navigate up from the application base directory (bin/Debug/net9.0) to the solution root
string solutionRoot = Path.GetFullPath(Path.Combine(
    AppContext.BaseDirectory, 
    "..", "..", "..", ".."
));

// Create the full path to the database file in the solution root
string dbPath = Path.Combine(solutionRoot, "SynapseEnqueteManagement.db");

// Configure SQLite to use the database file in the solution root
optionsBuilder.UseSqlite($"Data Source={dbPath}");

var context = new SynapseEnqueteContext(optionsBuilder.Options);

IRepository repository = new Repository(context);

bool dbCreated;
try
{
    // Create the database (delete first if it exists)
    dbCreated = context.CreateDatabase(deleteIfAlreadyExists);
}
catch (IOException ex)
{
    Console.WriteLine("Error: The database file is being used by another process.");
    Console.WriteLine("Please close any applications that might be accessing the database (e.g., DB Browser for SQLite).");
    Console.WriteLine($"Details: {ex.Message}");
    return; // Exit the program
}
if (dbCreated)
{
    DataSeeder.Seed(context);
}
//Composition Root
IManager manager = new Manager(repository);
ConsoleUi consoleUi = new ConsoleUi(manager);
consoleUi.Run();
