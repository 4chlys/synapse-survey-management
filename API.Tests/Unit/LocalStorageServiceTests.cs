using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using SynapseEnqueteManagement.API.Services;

namespace SynapseEnqueteManagement.API.Tests.Unit;

public class LocalStorageServiceTests : IDisposable
{
    private readonly string _webRoot;
    private readonly LocalStorageService _service;

    public LocalStorageServiceTests()
    {
        _webRoot = Path.Combine(Path.GetTempPath(), "synapse-storage-tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_webRoot);

        var env = new Mock<IWebHostEnvironment>(MockBehavior.Strict);
        env.SetupGet(e => e.WebRootPath).Returns(_webRoot);

        // DeleteAsync never touches the HttpContext, so an unconfigured accessor is fine here.
        _service = new LocalStorageService(env.Object, new Mock<IHttpContextAccessor>().Object, NullLogger<LocalStorageService>.Instance);
    }

    public void Dispose()
    {
        if (Directory.Exists(_webRoot))
            Directory.Delete(_webRoot, recursive: true);
    }

    private string CreateFile(string relativePath)
    {
        var fullPath = Path.Combine(_webRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, "content");
        return fullPath;
    }

    [Fact]
    public async Task DeleteAsync_SeedUrl_KeepsFileOnDisk()
    {
        // Regression: deleting a seeded entity must not remove the shared, app-bundled seed file
        // under wwwroot/seed — otherwise a restart can no longer re-seed the database.
        var filePath = CreateFile("seed/questions/images/study-stress.jpg");

        await _service.DeleteAsync("https://localhost:7001/seed/questions/images/study-stress.jpg");

        Assert.True(File.Exists(filePath));
    }

    [Fact]
    public async Task DeleteAsync_UploadUrl_DeletesFile()
    {
        var filePath = CreateFile("uploads/ideas/photo.jpg");

        await _service.DeleteAsync("https://localhost:7001/uploads/ideas/photo.jpg");

        Assert.False(File.Exists(filePath));
    }
}
