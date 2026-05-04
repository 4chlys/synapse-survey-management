namespace SynapseEnqueteManagement.API.Services;

public class LocalStorageService(IWebHostEnvironment env, IHttpContextAccessor httpContextAccessor, ILogger<LocalStorageService> logger) : IStorageService
{
    public async Task<string> UploadAsync(IFormFile file, string folder)
    {
        var uploadsDir = Path.Combine(env.WebRootPath, "uploads", folder);
        Directory.CreateDirectory(uploadsDir);

        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        var filePath = Path.Combine(uploadsDir, fileName);

        await using var stream = File.Create(filePath);
        await file.CopyToAsync(stream);

        var request = httpContextAccessor.HttpContext!.Request;
        return $"{request.Scheme}://{request.Host}/uploads/{folder}/{fileName}";
    }

    public Task<string> PromoteAsync(string tmpUrl)
    {
        // Local URL shape: {scheme}://{host}/uploads/tmp/{rest}
        // Promoted URL:    {scheme}://{host}/uploads/{rest}
        var uri = new Uri(tmpUrl);
        var path = uri.AbsolutePath; // "/uploads/tmp/ideas/file.jpg"
        const string tmpSegment = "/uploads/tmp/";
        if (!path.StartsWith(tmpSegment, StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(tmpUrl); // already permanent

        var rest = path[tmpSegment.Length..]; // "ideas/file.jpg"
        var srcPath = Path.Combine(env.WebRootPath, "uploads", "tmp", rest.Replace('/', Path.DirectorySeparatorChar));
        var dstPath = Path.Combine(env.WebRootPath, "uploads", rest.Replace('/', Path.DirectorySeparatorChar));

        Directory.CreateDirectory(Path.GetDirectoryName(dstPath)!);
        File.Move(srcPath, dstPath, overwrite: true);

        var permanentUrl = $"{uri.Scheme}://{uri.Authority}/uploads/{rest}";
        return Task.FromResult(permanentUrl);
    }

    public Task DeleteAsync(string url)
    {
        try
        {
            var uri = new Uri(url);
            var relativePath = uri.AbsolutePath.TrimStart('/');
            var filePath = Path.Combine(env.WebRootPath, relativePath.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(filePath))
                File.Delete(filePath);
        }
        catch (FileNotFoundException) { }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[Storage] Failed to delete local file for URL {Url}", url);
        }
        return Task.CompletedTask;
    }
}
